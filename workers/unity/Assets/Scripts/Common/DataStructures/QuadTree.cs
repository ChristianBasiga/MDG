using Improbable;
using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common.Datastructures
{

    // This will be a quad tree with 
    public class QuadTree
    {
        // Also need dimensions.
        // Maybe flow is I get QuadNodes, then using EntityId I get collider component
        // to get dimensions of that entity in that region.
        // I don't care for dimensions of each point within quad tree.
        // hell potentially from that quad tree I could create another to get the actual colliding entities.
        // Look into GJK as that will likely be better
        // either way broad phase, could be firs this, then dot product to see if potentially overlapping.
        // that won't result in false negative but false positives which I'll handle in narrow phase.
        public struct QuadNode
        {
            public EntityId entityId;
            public Vector3f position;
        }
        // Information about this region.
        int capacity;
        Vector3f dimensions;
        Vector3f center;
        // Four children representing regions wihin this region.
        QuadTree northWest;
        QuadTree northEast;
        QuadTree southWest;
        QuadTree southEast;

        Dictionary<EntityId, Vector3f> entitiesInThisRegion;
        #region Interfacing code
        public QuadTree(int capacity, Vector3f dimensions, Vector3f center)
        {
            this.capacity = capacity;
            this.dimensions = dimensions;
            this.center = center;
            entitiesInThisRegion = new Dictionary<EntityId, Vector3f>();
        }

        public List<QuadNode> FindEntities(Vector3f queryPosition)
        {
            List<QuadNode> entities = new List<QuadNode>();

            if (!IsWithinRegion(queryPosition))
            {
                return entities;
            }
            // Check if subdivided.
            if (northEast == null)
            {
                //Recursively find them. If not exist in region, returns empty so no change.
                entities.AddRange(northEast.FindEntities(queryPosition));
                entities.AddRange(northWest.FindEntities(queryPosition));
                entities.AddRange(southEast.FindEntities(queryPosition));
                entities.AddRange(southWest.FindEntities(queryPosition));
            }
            else
            {
                foreach(KeyValuePair<EntityId, Vector3f> keyValuePair in entitiesInThisRegion)
                {
                    QuadNode quadNode = new QuadNode
                    {
                        entityId = keyValuePair.Key,
                        position = keyValuePair.Value
                    };
                }
            }
            return entities;
        }

        // Instead of moving via initial and new, can just pass transformation?
        // Actually no matter what may need to go back up tree, so traversing downward twice is fine just incase.
        // Unless I have it be own
        public void MoveEntity(EntityId entityId, Vector3f originalPosition, Vector3f newPosition)
        {
            Remove(entityId, originalPosition);
            Insert(entityId, newPosition);
        }

        public bool Insert(EntityId entityId, Vector3f position)
        {
            if (!IsWithinRegion(position))
            {
                return false;
            }
            // If one region is undefined, then I haven't subdivided.
            if (northEast == null)
            {
                if (entitiesInThisRegion.ContainsKey(entityId))
                {
                    throw new System.Exception($" The entity with id {entityId} is already in this quadtree. " +
                        $"Please use MoveEntity insted");
                }
                if (entitiesInThisRegion.Count + 1 > capacity)
                {
                    SubDivide();
                }
            }
            // This is if we already subdivided before, or subdivided within this stack frame.
            if (northEast != null)
            {
                // Recursively call insert in children, until one of the calls results in true
                // short circuiting the rest.
                return northWest.Insert(entityId, position)
                    || northEast.Insert(entityId, position)
                    || southWest.Insert(entityId, position)
                    || southEast.Insert(entityId, position);
            }
            entitiesInThisRegion.Add(entityId, position);
            return false;
        }

        // Maybe should create private try remove.
        public bool Remove(EntityId entityId, Vector3f position)
        {
            if (!IsWithinRegion(position))
            {
                return false;
            }
            if (northEast == null)
            {
                return northWest.Remove(entityId, position)
                    || northEast.Remove(entityId, position)
                    || southEast.Remove(entityId, position)
                    || southWest.Remove(entityId, position);
            }
            else
            {
                entitiesInThisRegion.Remove(entityId);
                return true;
            }
        }


        #endregion


        #region Helper private functions

        // Just keeping it in interface functions fine, them returning bools NOT huge deal.
        private bool TryRemove(EntityId entityId, Vector3f position)
        {
            return false;
        }

        // Insantiates 4 children to create inner regions.
        private void SubDivide()
        {
            float width = dimensions.X;
            float height = dimensions.Z;
            Vector3f newDimensions = new Vector3f(width / 2, 0, height / 2);
            // Dimensions also needs to be halved.
            northEast = new QuadTree(capacity, newDimensions, new Vector3f(center.X + width / 4, 0, center.Z + height / 4 ));
            northWest = new QuadTree(capacity, newDimensions, new Vector3f(center.X - width / 4, 0, center.Z + height / 4));
            southEast = new QuadTree(capacity, newDimensions, new Vector3f(center.X + width / 4, 0,  center.Z - height / 4));
            southWest = new QuadTree(capacity, newDimensions, new Vector3f(center.X - width / 4, 0, center.Z - height / 4));
        }

        private bool IsWithinRegion(Vector3f position)
        {
            float width = dimensions.X;
            float height = dimensions.Z;

            return (position.X <= center.X + width / 2)
                && (position.X >= center.X - width / 2)
                && (position.Z <= center.Z + height / 2)
                && (position.Z >= center.Z - height / 2);
        }
        #endregion
    }
}