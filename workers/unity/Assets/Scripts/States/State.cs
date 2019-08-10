using System;
using Improbable.Gdk.Core;
namespace MDG.States
{
    public class State
    {
        public DateTime LastUpdated { get; set; }
        public bool UpdatedThisFrame { get; set; }
        public EntityId EntityID { get; set; }

        public virtual void reduce() { }
    }

    //This is wasted effoer, idk what even using this for.
    public class AccumilativeState: State
    {
        private State accum;
        public AccumilativeState(State accum)
        {
            this.accum = accum;
        }
        public override void reduce()
        {
            base.reduce();
        }
    }
}