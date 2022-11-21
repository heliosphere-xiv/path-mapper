namespace PathMapper {
    public enum FileType : byte {
        Unknown,
        Sound,
        Imc,
        Vfx,
        Animation,
        Pap,
        MetaInfo,
        Material,
        Texture,
        Model,
        Shader,
        Font,
        Environment,
        Skeleton,
        SkeletonParameter,
        ElementId,
        SkeletonPhysicsBinary,
    }

    public static partial class Names {
        public static readonly Dictionary<string, FileType> ExtensionToFileType = new() {
            { ".mdl", FileType.Model },
            { ".tex", FileType.Texture },
            { ".mtrl", FileType.Material },
            { ".atex", FileType.Animation },
            { ".avfx", FileType.Vfx },
            { ".scd", FileType.Sound },
            { ".imc", FileType.Imc },
            { ".pap", FileType.Pap },
            { ".eqp", FileType.MetaInfo },
            { ".eqdp", FileType.MetaInfo },
            { ".est", FileType.MetaInfo },
            { ".exd", FileType.MetaInfo },
            { ".exh", FileType.MetaInfo },
            { ".shpk", FileType.Shader },
            { ".shcd", FileType.Shader },
            { ".fdt", FileType.Font },
            { ".envb", FileType.Environment },
            { ".sklb", FileType.Skeleton },
            { ".skp", FileType.SkeletonParameter },
            { ".eid", FileType.ElementId },
            { ".phyb", FileType.SkeletonPhysicsBinary },
        };
    }
}
