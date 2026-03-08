using Unity.Entities;
using Unity.Mathematics;

namespace RuntimeRoguelike.Dots.Runtime
{
    /// <summary>
    /// Данные одной комнаты, хранится в BlobArray внутри MapBlob.
    /// </summary>
    public struct RoomBlob
    {
        /// <summary>(x, y, width, height) — bounding box комнаты.</summary>
        public int4 Bounds;
        /// <summary>Центр bounding box: (x + width/2, y + height/2).</summary>
        public int2 Center;
        /// <summary>Тип комнаты (RoomType enum, хранится как byte).</summary>
        public byte RoomType;
        /// <summary>Количество соединённых коридорами комнат.</summary>
        public byte ConnectedCount;
    }

    public struct MapBlob
    {
        public int2 Size;
        public int2 StartCell;

        // Существующие слои
        public BlobArray<MapCellType> BaseLayer;
        public BlobArray<ObstacleType> ObstacleLayer;
        public BlobArray<HazardType> HazardLayer;

        // Новые слои для визуального оформления
        /// <summary>4-bit bitmask стен для auto-tiling (N=1, E=2, S=4, W=8).</summary>
        public BlobArray<byte> WallMaskLayer;
        /// <summary>Индекс варианта пола (0..FloorVariantCount-1), по xxHash(seed, cell).</summary>
        public BlobArray<byte> FloorVariantLayer;

        // Данные комнат
        public int RoomCount;
        public BlobArray<RoomBlob> Rooms;
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
