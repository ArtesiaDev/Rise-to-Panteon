using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots
{
    public struct MapBlob
    {
        public int2 Size;
        public int2 StartCell;
        public BlobArray<MapCellType> BaseLayer;
        public BlobArray<ObstacleType> ObstacleLayer;
        public BlobArray<HazardType> HazardLayer;
    }

    public struct MapBlobReference : IComponentData
    {
        public BlobAssetReference<MapBlob> Value;
    }

    [InternalBufferCapacity(0)]
    public struct CellOccupant : IBufferElementData
    {
        public Entity Value;
    }

    public struct MapRenderRequest : IComponentData, IEnableableComponent
    {
        public int RunId;
    }
}
