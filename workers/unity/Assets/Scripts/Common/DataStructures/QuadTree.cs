using Improbable;
using Improbable.Gdk.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MDG.Common.Datastructures
{

    public class QuadNode
    {
        public EntityId entityId;
        public Vector3f position;
        public Vector3f center;
        public Vector3f dimensions;
        public override bool Equals(object obj)
        {
            QuadNode other = (QuadNode)obj;
            return entityId.Equals(other.entityId)
                && position.Equals(other.position)
                && center.Equals(other.center)
                && dimensions.Equals(other.dimensions);
        }
    }
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
        
        // Information about this region.
        int capacity;
        Vector3f dimensions;
        Vector3f center;

        QuadTree parent;
        // Four children representing regions wihin this region.
        QuadTree northWest;
        QuadTree northEast;
        QuadTree southWest;
        QuadTree southEast;

        Dictionary<EntityId, Vector3f> entitiesInThisRegion;
        #region Interfacing code
        public QuadTree(int capacity, Vector3f dimensions, Vector3f center, QuadTree parent = null)
        {
            this.parent = parent;
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
            if (northEast != null)
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
                        position = keyValuePair.Value,
                        dimensions = dimensions,
                        center = center
                    };
                    entities.Add(quadNode);
                }
            }
            return entities;
        }

        public QuadNode FindEntity(EntityId entityId)
        {
            QuadNode result = new QuadNode
            {
                entityId = entityId,
            };
            bool found = FindEntityUtil(entityId, result);

            if (found)
            {
                return result;
            }
            else
            {
                return null;
            }
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
            Debug.Log($"Inserting {entityId} with position {position}");
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
                    Debug.Log("Sub dividing");
                    SubDivide();
                }
            }
            // This is if we already subdivided before, or subdivided within this stack frame.
            if (northEast != null)
            {
                Debug.Log("recursively inserting");
                // Recursively call insert in children, until one of the calls results in true
                // short circuiting the rest.
                return northWest.Insert(entityId, position)
                    || northEast.Insert(entityId, position)
                    || southWest.Insert(entityId, position)
                    || southEast.Insert(entityId, position);
            }
            Debug.Log($"Adding into this region with center {center.ToString()} and dimensions {dimensions.ToString()} ");
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
            if (northEast != null)
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

        public bool Remove(EntityId entityId)
        {

            if (entitiesInThisRegion.TryGetValue(entityId, out Vector3f currPos))
            {
                entitiesInThisRegion.Remove(entityId);
                return true;
            }

            if (northEast != null)
            {
                return northEast.Remove(entityId)
                   || northWest.Remove(entityId)
                   || southEast.Remove(entityId)
                   || southWest.Remove(entityId);
            }

            return false;
        }

        #endregion


        #region Helper private functions


        private bool FindEntityUtil(EntityId id, QuadNode node)
        {
            if (entitiesInThisRegion.TryGetValue(id, out Vector3f currPos))
            {
                node.position = currPos;
                node.center = center;
                node.dimensions = dimensions;
                return true;
            }

            if (northEast != null)
            {
                return northEast.FindEntityUtil(id, node)
                    || northWest.FindEntityUtil(id, node)
                    || southEast.FindEntityUtil(id, node)
                    || southWest.FindEntityUtil(id, node);
            }

            return false;
        }

        // Just keeping it in interface functions fine, them returning bools NOT huge deal.
        private bool TryRemove(EntityId entityId, Vector3f position)
        {
            return false;
        }

        // Insantiates 4 children to create inner regions.
        // Here is peace I'm forgetting.
        // When I subdivide, they need to move to divisions.
        private void SubDivide()
        {
            float width = dimensions.X;
            float height = dimensions.Z;
            // Technically can subdivide indefinitely, but no need.
            if (width <= 1 || height <= 1)
            {
                // Replace with my custom logger.
                UnityEngine.Debug.Log("At unit size, cannot subdivide further");
            }
            Vector3f newDimensions = new Vector3f(width / 2, 0, height / 2);
            //Dimensions also needs to be halved.
            //Subdividing all at once, makes  pruning a little harder, but reduces duplicate code.
            northEast = new QuadTree(capacity, newDimensions, new Vector3f(center.X + width / 4, 0, center.Z + height / 4 ));
            northWest = new QuadTree(capacity, newDimensions, new Vector3f(center.X - width / 4, 0, center.Z + height / 4));
            southEast = new QuadTree(capacity, newDimensions, new Vector3f(center.X + width / 4, 0,  center.Z - height / 4));
            southWest = new QuadTree(capacity, newDimensions, new Vector3f(center.X - width / 4, 0, center.Z - height / 4));

            foreach (KeyValuePair<EntityId, Vector3f> keyValuePair in entitiesInThisRegion)
            {
                bool insertedInSubdivision = northEast.Insert(keyValuePair.Key, keyValuePair.Value)
                || northWest.Insert(keyValuePair.Key, keyValuePair.Value)
                || southEast.Insert(keyValuePair.Key, keyValuePair.Value)
                || southWest.Insert(keyValuePair.Key, keyValuePair.Value);
            }
            entitiesInThisRegion.Clear();
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