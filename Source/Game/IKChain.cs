using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Game
{
    public class IKChain : Script
    {
        public Actor Target;
        public IKJoint RootJoint;

        public override void OnUpdate()
        {
            if (!Target || !RootJoint) return;
            var target = Target.Transform.Translation;
            RootJoint.Evaluate(ref target);
        }
    }
}
