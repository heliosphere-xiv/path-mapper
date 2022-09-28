using System.ComponentModel;

namespace PathMapper {
    public enum CustomizationType : byte {
        Unknown,
        Body,
        Legs,
        Gloves,
        Shoes,
        Tail,
        Face,
        Iris,
        Accessory,
        Hair,
        Zear,
        DecalFace,
        DecalEquip,
        Skin,
        Etc,
    }

    public static class CustomizationTypeEnumExtension {
        public static string ToSuffix(this CustomizationType value) {
            return value switch {
                CustomizationType.Body => "top",
                CustomizationType.Legs => "dwn",
                CustomizationType.Gloves => "glv",
                CustomizationType.Shoes => "sho",
                CustomizationType.Face => "fac",
                CustomizationType.Iris => "iri",
                CustomizationType.Accessory => "acc",
                CustomizationType.Hair => "hir",
                CustomizationType.Tail => "til",
                CustomizationType.Zear => "zer",
                CustomizationType.Etc => "etc",
                _ => throw new InvalidEnumArgumentException(),
            };
        }
    }

    public static partial class Names {
        public static readonly Dictionary<string, CustomizationType> SuffixToCustomizationType = new() {
            { CustomizationType.Body.ToSuffix(), CustomizationType.Body },
            { CustomizationType.Legs.ToSuffix(), CustomizationType.Legs },
            { CustomizationType.Gloves.ToSuffix(), CustomizationType.Gloves },
            { CustomizationType.Shoes.ToSuffix(), CustomizationType.Shoes },
            { CustomizationType.Face.ToSuffix(), CustomizationType.Face },
            { CustomizationType.Iris.ToSuffix(), CustomizationType.Iris },
            { CustomizationType.Accessory.ToSuffix(), CustomizationType.Accessory },
            { CustomizationType.Hair.ToSuffix(), CustomizationType.Hair },
            { CustomizationType.Tail.ToSuffix(), CustomizationType.Tail },
            { CustomizationType.Zear.ToSuffix(), CustomizationType.Zear },
            { CustomizationType.Etc.ToSuffix(), CustomizationType.Etc },
        };
    }
}
