using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using System;

namespace Stride.IK
{
    public class IKProcessor : EntityProcessor<IKComponent>
    {
        public IKProcessor()
        {
            Order = 0;
        }
        public override void Draw(RenderContext context)
        {
            foreach(var c in ComponentDatas)
            {
                try
                {
                    var s = c.Key.Entity.Get<ModelComponent>().Skeleton;
                    var x = c.Key.Entity.EntityManager.Processors;
                    c.Key.ComputeIK(context.Time);

                } catch (Exception)
                {
                    //
                }
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] IKComponent component, [NotNull] IKComponent data)
        {
            component.BuildGraph();
            base.OnEntityComponentAdding(entity, component, data);
        }
        protected override bool IsAssociatedDataValid(Entity entity, IKComponent component, IKComponent associatedData)
        {
            return component.CheckValid();
        }


        public override void Update(GameTime time)
        {
            base.Update(time);
        }

        protected override IKComponent GenerateComponentData(Entity entity, IKComponent component)
        {
            return base.GenerateComponentData(entity, component);
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] IKComponent component, [NotNull] IKComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
        }

        protected override void OnSystemAdd()
        {
            base.OnSystemAdd();
        }

        protected override void OnSystemRemove()
        {
            base.OnSystemRemove();
        }

        protected override void ProcessEntityComponent(Entity entity, EntityComponent entityComponentArg, bool forceRemove)
        {
            base.ProcessEntityComponent(entity, entityComponentArg, forceRemove);
        }

        protected override void RemoveAllEntities()
        {
            base.RemoveAllEntities();
        }
    }
}