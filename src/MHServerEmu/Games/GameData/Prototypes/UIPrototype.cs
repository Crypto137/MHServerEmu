using MHServerEmu.Common.Extensions;
using MHServerEmu.Games.Common;
using MHServerEmu.Games.GameData.Resources;

namespace MHServerEmu.Games.GameData.Prototypes
{
    public enum PanelScaleMode
    {
        None,
        XStretch,
        YOnly,
        XOnly,
        Both,
        ScreenSize
    }

    public class UIPrototype
    {
        public ResourceHeader Header { get; }
        public UIPanelPrototype[] UIPanels { get; }

        public UIPrototype(byte[] data)
        {
            using (MemoryStream stream = new(data))
            using (BinaryReader reader = new(stream))
            {
                Header = new(reader);

                UIPanels = new UIPanelPrototype[reader.ReadUInt32()];
                for (int i = 0; i < UIPanels.Length; i++)
                    UIPanels[i] = UIPanelPrototype.ReadFromBinaryReader(reader);
            }
        }
    }

    public class UIPanelPrototype
    {
        public ResourcePrototypeHash ProtoNameHash { get; protected set; }
        public string PanelName { get; protected set; }
        public string TargetName { get; protected set; }
        public PanelScaleMode ScaleMode { get; protected set; }
        public UIPanelPrototype Children { get; protected set; }
        public string WidgetClass { get; protected set; }
        public string SwfName { get; protected set; }
        public byte OpenOnStart { get; protected set; }
        public byte VisibilityToggleable { get; protected set; }
        public byte CanClickThrough { get; protected set; }
        public byte StaticPosition { get; protected set; }
        public byte EntityInteractPanel { get; protected set; }
        public byte UseNewPlacementSystem { get; protected set; }
        public byte KeepLoaded { get; protected set; }

        public static UIPanelPrototype ReadFromBinaryReader(BinaryReader reader)
        {
            ResourcePrototypeHash hash = (ResourcePrototypeHash)reader.ReadUInt32();

            switch (hash)
            {
                case ResourcePrototypeHash.StretchedPanelPrototype:
                    return new StretchedPanelPrototype(reader);
                case ResourcePrototypeHash.AnchoredPanelPrototype:
                    return new AnchoredPanelPrototype(reader);
                case ResourcePrototypeHash.None:
                    return null;
                default:
                    throw new($"Unknown ResourcePrototypeHash {(uint)hash}");   // Throw an exception if there's a hash for a type we didn't expect
            }
        }

        protected void ReadCommonPanelFields(BinaryReader reader)
        {
            PanelName = reader.ReadFixedString32();
            TargetName = reader.ReadFixedString32();
            ScaleMode = (PanelScaleMode)reader.ReadUInt32();
            Children = ReadFromBinaryReader(reader);
            WidgetClass = reader.ReadFixedString32();
            SwfName = reader.ReadFixedString32();
            OpenOnStart = reader.ReadByte();
            VisibilityToggleable = reader.ReadByte();
            CanClickThrough = reader.ReadByte();
            StaticPosition = reader.ReadByte();
            EntityInteractPanel = reader.ReadByte();
            UseNewPlacementSystem = reader.ReadByte();
            KeepLoaded = reader.ReadByte();
        }
    }

    public class StretchedPanelPrototype : UIPanelPrototype
    {
        public Vector2 TopLeftPin { get; }
        public string TL_X_TargetName { get; }
        public string TL_Y_TargetName { get; }
        public Vector2 BottomRightPin { get; }
        public string BR_X_TargetName { get; }
        public string BR_Y_TargetName { get; }

        public StretchedPanelPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.StretchedPanelPrototype;

            TopLeftPin = reader.ReadVector2();
            TL_X_TargetName = reader.ReadFixedString32();
            TL_Y_TargetName = reader.ReadFixedString32();
            BottomRightPin = reader.ReadVector2();
            BR_X_TargetName = reader.ReadFixedString32();
            BR_Y_TargetName = reader.ReadFixedString32();

            ReadCommonPanelFields(reader);
        }
    }

    public class AnchoredPanelPrototype : UIPanelPrototype
    {
        public Vector2 SourceAttachmentPin { get; }
        public Vector2 TargetAttachmentPin { get; }
        public Vector2 VirtualPixelOffset { get; }
        public string PreferredLane { get; }
        public Vector2 OuterEdgePin { get; }
        public Vector2 NewSourceAttachmentPin { get; }

        public AnchoredPanelPrototype(BinaryReader reader)
        {
            ProtoNameHash = ResourcePrototypeHash.AnchoredPanelPrototype;

            SourceAttachmentPin = reader.ReadVector2();
            TargetAttachmentPin = reader.ReadVector2();
            VirtualPixelOffset = reader.ReadVector2();
            PreferredLane = reader.ReadFixedString32();
            OuterEdgePin = reader.ReadVector2();
            NewSourceAttachmentPin = reader.ReadVector2();

            ReadCommonPanelFields(reader);
        }
    }
}
