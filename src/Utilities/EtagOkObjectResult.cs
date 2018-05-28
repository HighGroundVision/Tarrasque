using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace HGV.Tarrasque.Utilities
{
    public class EtagOkObjectResult : OkObjectResult
    {
        public EntityTagHeaderValue ETag { get; set; }

        public EtagOkObjectResult(object value) : base(value)
        {
        }

        public override void ExecuteResult(ActionContext context)
        {
            this.AddETag(context);

            base.ExecuteResult(context);
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            this.AddETag(context);

            return base.ExecuteResultAsync(context);
        }

        private void AddETag(ActionContext context)
        {
            if (this.ETag != null)
            {
                context.HttpContext.Response.Headers.Add("ETag", this.ETag.Tag.Value);
            }
        }
    }
}
