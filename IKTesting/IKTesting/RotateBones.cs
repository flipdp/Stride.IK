using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;

namespace IKTesting
{
    public class RotateBones : SyncScript
    {
        // Declared public member fields and properties will show in the game studio
        public Entity target;
        public override void Start()
        {
            // Initialization of the script.
        }

        public override void Update()
        {
            var sk = Entity.Get<ModelComponent>().Skeleton;
            // Quaternion.RotationYawPitchRoll()
            DebugText.Print(Game.UpdateTime.Total.ToString(), new Int2(10,20));
            var i = 24;
            var n = sk.NodeTransformations[i];
            var cnPos = sk.NodeTransformations[i+1].WorldMatrix.TranslationVector;
            var npos = n.WorldMatrix.TranslationVector;
            var tpos = target.Transform.Position;
            var p = Quaternion.RotationMatrix(sk.NodeTransformations[n.ParentIndex].WorldMatrix); p.Invert();
            
            var rot = Quaternion.RotationMatrix(Matrix.LookAtRH(npos,npos + Vector3.UnitY,Vector3.UnitY));
            // rot = Quaternion.BetweenDirections(n.WorldMatrix.Forward,tpos-npos);
            // rot = Quaternion.RotationYawPitchRoll(rot.YawPitchRoll.X,rot.YawPitchRoll.Y,0);
            // sk.NodeTransformations[24].Transform.Rotation = rot;
        }
        public Quaternion LookAt(Vector3 origin, Vector3 target)
        {
            float azimuth = GetLookAtAngles(origin, target, out float altitude);
            return Quaternion.RotationYawPitchRoll(azimuth, -altitude, 0);
        }
        private static float GetLookAtAngles(Vector3 source, Vector3 destination, out float altitude)
        {
            var x = source.X - destination.X;
            var y = source.Y - destination.Y;
            var z = source.Z - destination.Z;

            altitude = (float)Math.Atan2(y, Math.Sqrt(x * x + z * z));
            var azimuth = (float)Math.Atan2(x, z);
            return azimuth;
        }
    }
}
