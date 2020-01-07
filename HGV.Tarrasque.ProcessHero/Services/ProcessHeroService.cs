using Dawn;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.Common.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HGV.Tarrasque.ProcessHero.Services
{
    public interface IProcessHeroService
    {
        Task ProcessHero(HeroReference heroRef, TextReader reader, TextWriter writer);
    }

    public class ProcessHeroService : IProcessHeroService
    {
        public ProcessHeroService()
        {
        }

        public async Task ProcessHero(HeroReference heroRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(heroRef, nameof(heroRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            if (reader == null)
                await NewHero(heroRef, writer);
            else
                await UpdateHero(heroRef, reader, writer);
        }

        private static async Task UpdateHero(HeroReference heroRef, TextReader reader, TextWriter writer)
        {
            Guard.Argument(heroRef, nameof(heroRef)).NotNull();
            Guard.Argument(reader, nameof(reader)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            throw new NotImplementedException();
        }

        private static async Task NewHero(HeroReference heroRef, TextWriter writer)
        {
            Guard.Argument(heroRef, nameof(heroRef)).NotNull();
            Guard.Argument(writer, nameof(writer)).NotNull();

            throw new NotImplementedException();
        }
    }
}
