using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessCheckpoint.Entities
{
    public enum CircuitState
    {
        Closed = 0,
        Open = 1,
        HalfOpen = 2,
    }

    public interface IDurableBreaker
    {
        Task<CircuitState> RecordSuccess();
        Task<CircuitState> RecordFailure();
        Task<CircuitState> GetCircuitState();
        Task<CircuitBreaker> GetBreakerState();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CircuitBreaker : IDurableBreaker
    {
        public CircuitBreaker()
        {
        }

        public CircuitBreaker(ILogger log)
        {
        }

        [JsonProperty]
        public DateTime BrokenUntil { get; set; }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public CircuitState CircuitState { get; set; }

        [JsonProperty]
        public int ConsecutiveFailureCount { get; set; }

        [JsonProperty]
        public int MaxConsecutiveFailures { get; set; }

        [JsonProperty]
        public TimeSpan BreakDuration { get; set; }

        public Task<CircuitState> RecordSuccess()
        {
            ConsecutiveFailureCount = 0;

            // A success result in HalfOpen state causes the circuit to close (permit executions) again.
            if (IsHalfOpen())
            {
                BrokenUntil = DateTime.MinValue;
                CircuitState = CircuitState.Closed;
            }

            return Task.FromResult(CircuitState);
        }

        public Task<CircuitState> RecordFailure()
        {
            ConsecutiveFailureCount++;

            // If we have too many consecutive failures, open the circuit.
            // Or a failure when in the HalfOpen 'testing' state? That also breaks the circuit again.
            if (
                (CircuitState == CircuitState.Closed && ConsecutiveFailureCount >= MaxConsecutiveFailures) || IsHalfOpen())
            {
                CircuitState = CircuitState.Open;
                BrokenUntil = DateTime.UtcNow + BreakDuration;
            }

            return Task.FromResult(CircuitState);
        }

        public Task<CircuitState> GetCircuitState()
        {
            return Task.FromResult(CircuitState);
        }

        public Task<CircuitBreaker> GetBreakerState()
        {
            return Task.FromResult(this);
        }

        /// <summary>
        /// Function entry point; d
        /// </summary>
        /// <param name="context">An <see cref="IDurableEntityContext"/>, provided by dependency-injection.</param>
        /// <param name="logger">An <see cref="ILogger"/>, provided by dependency-injection.</param>
        [FunctionName(nameof(CircuitBreaker))]
        public static async Task Run([EntityTrigger] IDurableEntityContext context, ILogger logger)
        {
            // The first time the circuit-breaker is accessed, it will self-configure.
            if (!context.HasState)
            {
                var breaker = new CircuitBreaker()
                {
                    CircuitState = CircuitState.Closed,
                    BrokenUntil = DateTime.MinValue,
                    ConsecutiveFailureCount = 0,
                    MaxConsecutiveFailures = 5,
                    BreakDuration = TimeSpan.FromMinutes(10)
                };
                context.SetState(breaker);
            }

            await context.DispatchAsync<CircuitBreaker>(logger);
        }

        private bool IsHalfOpen()
        {
            return CircuitState == CircuitState.HalfOpen || CircuitState == CircuitState.Open && DateTime.UtcNow > BrokenUntil;
        }
    }

    public partial class DurableCircuitBreakerClient
    {
        private readonly EntityId circuitBreakerId;

        public DurableCircuitBreakerClient(string instance)
        {
            circuitBreakerId = new EntityId(nameof(CircuitBreaker), instance);
        }

        public async Task<bool> IsExecutionPermitted(IDurableClient durableClient, ILogger log)
        {
            // The performance priority approach reads the circuit-breaker entity state from outside.
            // Per Azure Entity Functions documentation, this may be stale if other operations on the entity have been queued but not yet actioned,
            // but it returns faster than actually executing an operation on the entity (which would queue as a serialized operation against others).

            // The trade-off is that a true half-open state (permitting only one execution per breakDuration) cannot be maintained.

            var breakerState = await GetBreakerState(durableClient, log);

            bool isExecutionPermitted;
            if (breakerState == null)
            {
                // We permit execution if the breaker is not yet initialized; a not-yet-initialized breaker is deemed closed, for simplicity.
                // It will be initialized when the first success or failure is recorded against it.
                isExecutionPermitted = true;
            }
            else if (breakerState.CircuitState == CircuitState.HalfOpen || breakerState.CircuitState == CircuitState.Open)
            {
                // If the circuit is open or half-open, we permit executions if the broken-until period has passed.
                // Unlike the Consistency mode, we cannot control (since we only read state, not update it) how many executions are permitted in this state.
                // However, the first execution to fail in half-open state will push out the BrokenUntil time by BreakDuration, blocking executions until the next BreakDuration has passed.
                // (Or a success first will close the circuit again.)
                isExecutionPermitted = DateTime.UtcNow > breakerState.BrokenUntil;
            }
            else if (breakerState.CircuitState == CircuitState.Closed)
            {
                isExecutionPermitted = true;
            }
            else
            {
                throw new InvalidOperationException();
            }

            return isExecutionPermitted;
        }

        public async Task RecordSuccess(IDurableClient durableClient, ILogger log)
        {
            await durableClient.SignalEntityAsync<IDurableBreaker>(circuitBreakerId, breaker => breaker.RecordSuccess());
        }

        public async Task RecordFailure(IDurableClient durableClient, ILogger log)
        {
            await durableClient.SignalEntityAsync<IDurableBreaker>(circuitBreakerId, breaker => breaker.RecordFailure());
        }

        public async Task<CircuitBreaker?> GetBreakerState(IDurableClient durableClient, ILogger log)
        {
            var readState = await durableClient.ReadEntityStateAsync<CircuitBreaker>(circuitBreakerId);

            // We present a not-yet-initialized circuit-breaker as null (it will be initialized when successes or failures are first posted against it).
            if (!readState.EntityExists || readState.EntityState == null)
            {
                return null;
            }

            return readState.EntityState;
        }
    }
}
