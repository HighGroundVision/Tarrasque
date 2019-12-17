using System;
using System.Collections.Generic;
using System.Text;

namespace Tarrasque.Collection
{
    public interface IMyService
    {
        void DoStuff();
    }

    public class MyService : IMyService
    {
        public void DoStuff()
        {

        }
    }
}
