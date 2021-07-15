using System;

namespace OvergownBot
{
    public abstract class BaseSheet
    {
        public string Name { get; private set; }
        
        protected Context _ctx;

        public BaseSheet(string name)
        {
            Name = name;
        }

        public virtual void Init(Context ctx)
        {
            _ctx = ctx;
        }
        
    }
}