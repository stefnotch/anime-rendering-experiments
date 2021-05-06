using FlaxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class IKChain : Script
    {
        /// <summary>
        /// The target actor that this chain should move towards
        /// </summary>
        public Actor Target;

        /// <summary>
        /// The first joint in the IK chain
        /// </summary>
        public IKJoint RootJoint;

        /// <summary>
        /// The actor at the end of the IK chain
        /// </summary>
        public Actor EndJoint;

        public override void OnUpdate()
        {
            if (!Target || !RootJoint || !EndJoint) return;
            var target = Target.Transform.Translation;
            RootJoint.Evaluate(EndJoint, ref target);
        }
    }
}
