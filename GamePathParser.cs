using System.Text;
using System.Text.RegularExpressions;
using Dalamud;
using Lumina;
using Lumina.Data.Files;

namespace PathMapper;

internal class GamePathParser {
    private static GameData _gameData;

    internal GamePathParser(GameData gameData) {
        _gameData = gameData;
    }

    private const string CharacterFolder = "chara";
    private const string EquipmentFolder = "equipment";
    private const string PlayerFolder = "human";
    private const string WeaponFolder = "weapon";
    private const string AccessoryFolder = "accessory";
    private const string DemiHumanFolder = "demihuman";
    private const string MonsterFolder = "monster";
    private const string CommonFolder = "common";
    private const string UiFolder = "ui";
    private const string IconFolder = "icon";
    private const string LoadingFolder = "loadingimage";
    private const string MapFolder = "map";
    private const string InterfaceFolder = "uld";
    private const string FontFolder = "font";
    private const string HousingFolder = "hou";
    private const string VfxFolder = "vfx";
    private const string WorldFolder1 = "bgcommon";
    private const string WorldFolder2 = "bg";

    private readonly Dictionary<FileType, Dictionary<ObjectType, Regex[]>> _regexes = new() {
        [FileType.Font] = new Dictionary<ObjectType, Regex[]> {
            [ObjectType.Font] = new Regex[] {
                new(@"common/font/(?'fontname'.*)_(?'id'\d\d)(_lobby)?\.fdt", RegexOptions.Compiled),
            },
        },
        [FileType.Texture] = new Dictionary<ObjectType, Regex[]> {
            [ObjectType.Icon] = new Regex[] {
                new(@"ui/icon/(?'group'\d*)(/(?'lang'[a-z]{2}))?(/(?'hq'hq))?/(?'id'\d*)(?'hr'_hr1)?\.tex", RegexOptions.Compiled),
            },
            [ObjectType.Map] = new Regex[] {
                new(@"ui/map/(?'id'[a-z0-9]{4})/(?'variant'\d{2})/\k'id'\k'variant'(?'suffix'[a-z])?(_[a-z])?\.tex", RegexOptions.Compiled),
            },
            [ObjectType.Weapon] = new Regex[] {
                new(@"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/texture/v(?'variant'\d{2})_w\k'id'b\k'weapon'(_[a-z])?_[a-z]\.tex", RegexOptions.Compiled),
            },
            [ObjectType.Monster] = new Regex[] {
                new(@"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/texture/v(?'variant'\d{2})_m\k'monster'b\k'id'(_[a-z])?_[a-z]\.tex", RegexOptions.Compiled),
            },
            [ObjectType.Equipment] = new Regex[] {
                new(@"chara/equipment/e(?'id'\d{4})/texture/v(?'variant'\d{2})_c(?'race'\d{4})e\k'id'_(?'slot'[a-z]{3})(_[a-z])?_[a-z]\.tex", RegexOptions.Compiled),
            },
            [ObjectType.DemiHuman] = new Regex[] {
                new(@"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/texture/v(?'variant'\d{2})_d\k'id'e\k'equip'_(?'slot'[a-z]{3})(_[a-z])?_[a-z]\.tex", RegexOptions.Compiled),
            },
            [ObjectType.Accessory] = new Regex[] {
                new(@"chara/accessory/a(?'id'\d{4})/texture/v(?'variant'\d{2})_c(?'race'\d{4})a\k'id'_(?'slot'[a-z]{3})_[a-z]\.tex", RegexOptions.Compiled),
            },
            [ObjectType.Character] = new Regex[] {
                new(@"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/texture/(?'minus'(--)?)(v(?'variant'\d{2})_)?c\k'race'\k'typeabr'\k'id'(_(?'slot'[a-z]{3}))?(_[a-z])?_[a-z]\.tex", RegexOptions.Compiled),
                new(@"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/texture", RegexOptions.Compiled),
                new(@"chara/common/texture/skin(?'skin'.*)\.tex", RegexOptions.Compiled),
                new(@"chara/common/texture/(?'catchlight'catchlight)(.*)\.tex", RegexOptions.Compiled),
                new(@"chara/common/texture/decal_(?'location'[a-z]+)/[-_]?decal_(?'id'\d+).tex", RegexOptions.Compiled),
            },
        },
        [FileType.Model] = new Dictionary<ObjectType, Regex[]> {
            [ObjectType.Weapon] = new Regex[] {
                new(@"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/model/w\k'id'b\k'weapon'\.mdl", RegexOptions.Compiled),
            },
            [ObjectType.Monster] = new Regex[] {
                new(@"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/model/m\k'monster'b\k'id'\.mdl", RegexOptions.Compiled),
            },
            [ObjectType.Equipment] = new Regex[] {
                new(@"chara/equipment/e(?'id'\d{4})/model/c(?'race'\d{4})e\k'id'_(?'slot'[a-z]{3})\.mdl", RegexOptions.Compiled),
            },
            [ObjectType.DemiHuman] = new Regex[] {
                new(@"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/model/d\k'id'e\k'equip'_(?'slot'[a-z]{3})\.mdl", RegexOptions.Compiled),
            },
            [ObjectType.Accessory] = new Regex[] {
                new(@"chara/accessory/a(?'id'\d{4})/model/c(?'race'\d{4})a\k'id'_(?'slot'[a-z]{3})\.mdl", RegexOptions.Compiled),
            },
            [ObjectType.Character] = new Regex[] {
                new(@"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/model/c\k'race'\k'typeabr'\k'id'_(?'slot'[a-z]{3})\.mdl", RegexOptions.Compiled),
            },
        },
        [FileType.Material] = new Dictionary<ObjectType, Regex[]>() {
            [ObjectType.Weapon] = new Regex[] {
                new(@"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/material/v(?'variant'\d{4})/mt_w\k'id'b\k'weapon'_[a-z]\.mtrl", RegexOptions.Compiled),
            },
            [ObjectType.Monster] = new Regex[] {
                new(@"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/material/v(?'variant'\d{4})/mt_m\k'monster'b\k'id'_[a-z]\.mtrl", RegexOptions.Compiled),
            },
            [ObjectType.Equipment] = new Regex[] {
                new(@"chara/equipment/e(?'id'\d{4})/material/v(?'variant'\d{4})/mt_c(?'race'\d{4})e\k'id'_(?'slot'[a-z]{3})_[a-z]\.mtrl", RegexOptions.Compiled),
            },
            [ObjectType.DemiHuman] = new Regex[] {
                new(@"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/material/v(?'variant'\d{4})/mt_d\k'id'e\k'equip'_(?'slot'[a-z]{3})_[a-z]\.mtrl", RegexOptions.Compiled),
            },
            [ObjectType.Accessory] = new Regex[] {
                new(@"chara/accessory/a(?'id'\d{4})/material/v(?'variant'\d{4})/mt_c(?'race'\d{4})a\k'id'_(?'slot'[a-z]{3})_[a-z]\.mtrl", RegexOptions.Compiled),
            },
            [ObjectType.Character] = new Regex[] {
                new(@"chara/human/c(?'race'\d{4})/obj/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/material(/v(?'variant'\d{4}))?/mt_c\k'race'\k'typeabr'\k'id'(_(?'slot'[a-z]{3}))?_[a-z]\.mtrl", RegexOptions.Compiled),
            },
        },
        [FileType.Imc] = new Dictionary<ObjectType, Regex[]> {
            [ObjectType.Weapon] = new Regex[] {
                new(@"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/b\k'weapon'\.imc", RegexOptions.Compiled),
            },
            [ObjectType.Monster] = new Regex[] {
                new(@"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/b\k'id'\.imc", RegexOptions.Compiled),
            },
            [ObjectType.Equipment] = new Regex[] {
                new(@"chara/equipment/e(?'id'\d{4})/e\k'id'\.imc", RegexOptions.Compiled),
            },
            [ObjectType.DemiHuman] = new Regex[] {
                new(@"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/e\k'equip'\.imc", RegexOptions.Compiled),
            },
            [ObjectType.Accessory] = new Regex[] {
                new(@"chara/accessory/a(?'id'\d{4})/a\k'id'\.imc", RegexOptions.Compiled),
            },
        },
        [FileType.Skeleton] = new Dictionary<ObjectType, Regex[]> {
            [ObjectType.Weapon] = new Regex[] {
                new(@"chara/weapon/w(?'id'\d{4})/skeleton/base/b(?'weapon'\d{4})/skl_w\k'id'b\k'weapon'\.sklb", RegexOptions.Compiled),
            },
            [ObjectType.Monster] = new Regex[] {
                new(@"chara/monster/m(?'monster'\d{4})/skeleton/base/b(?'id'\d{4})/skl_m\k'monster'b\k'id'\.sklb", RegexOptions.Compiled),
            },
            [ObjectType.DemiHuman] = new Regex[] {
                new(@"chara/demihuman/d(?'id'\d{4})/skeleton/base/b(?'equip'\d{4})/skl_d\k'id'b\k'equip'\.sklb", RegexOptions.Compiled),
            },
            [ObjectType.Character] = new Regex[] {
                new(@"chara/human/c(?'race'\d{4})/skeleton/(?'type'[a-z]+)/(?'typeabr'[a-z])(?'id'\d{4})/skl_c\k'race'm\k'id'\.sklb", RegexOptions.Compiled),
            },
        },
        [FileType.Vfx] = new Dictionary<ObjectType, Regex[]> {
            [ObjectType.Weapon] = new Regex[] {
                new(@"chara/weapon/w(?'id'\d{4})/obj/body/b(?'weapon'\d{4})/vfx/eff/vw(?'effect'\d{4})\.avfx", RegexOptions.Compiled),
            },
            [ObjectType.Equipment] = new Regex[] {
                new(@"chara/equipment/e(?'id'\d{4})/vfx/eff/ve(?'effect'\d{4})\.avfx", RegexOptions.Compiled),
            },
            [ObjectType.Monster] = new Regex[] {
                new(@"chara/monster/m(?'monster'\d{4})/obj/body/b(?'id'\d{4})/vfx/eff/vm(?'effect'\d{4})\.avfx", RegexOptions.Compiled),
            },
            [ObjectType.DemiHuman] = new Regex[] {
                new(@"chara/demihuman/d(?'id'\d{4})/obj/equipment/e(?'equip'\d{4})/vfx/eff/ve(?'effect'\d{4})\.avfx", RegexOptions.Compiled),
            },
        },
    };

    public ObjectType PathToObjectType(string path) {
        if (path.Length == 0) {
            return ObjectType.Unknown;
        }

        var folders = path.Split('/');
        if (folders.Length < 2) {
            return ObjectType.Unknown;
        }

        return folders[0] switch {
            CharacterFolder => folders[1] switch {
                EquipmentFolder => ObjectType.Equipment,
                AccessoryFolder => ObjectType.Accessory,
                WeaponFolder => ObjectType.Weapon,
                PlayerFolder => ObjectType.Character,
                DemiHumanFolder => ObjectType.DemiHuman,
                MonsterFolder => ObjectType.Monster,
                CommonFolder => ObjectType.Character,
                _ => ObjectType.Unknown,
            },
            UiFolder => folders[1] switch {
                IconFolder => ObjectType.Icon,
                LoadingFolder => ObjectType.LoadingScreen,
                MapFolder => ObjectType.Map,
                InterfaceFolder => ObjectType.Interface,
                _ => ObjectType.Unknown,
            },
            CommonFolder => folders[1] switch {
                FontFolder => ObjectType.Font,
                _ => ObjectType.Unknown,
            },
            HousingFolder => ObjectType.Housing,
            WorldFolder1 => folders[1] switch {
                HousingFolder => ObjectType.Housing,
                _ => ObjectType.World,
            },
            WorldFolder2 => ObjectType.World,
            VfxFolder => ObjectType.Vfx,
            _ => ObjectType.Unknown,
        };
    }

    private (FileType, ObjectType, Match?) ParseGamePath(string path) {
        if (!Names.ExtensionToFileType.TryGetValue(Extension(path), out var fileType)) {
            fileType = FileType.Unknown;
        }

        var objectType = this.PathToObjectType(path);

        if (!this._regexes.TryGetValue(fileType, out var objectDict)) {
            return (fileType, objectType, null);
        }

        if (!objectDict.TryGetValue(objectType, out var regexes)) {
            return (fileType, objectType, null);
        }

        foreach (var regex in regexes) {
            var match = regex.Match(path);
            if (match.Success) {
                return (fileType, objectType, match);
            }
        }

        return (fileType, objectType, null);
    }

    private static string Extension(string filename) {
        var extIdx = filename.LastIndexOf('.');
        return extIdx < 0 ? "" : filename[extIdx..];
    }

    private static IEnumerable<GameObjectInfo> HandleEquipmentVfx(FileType fileType, GroupCollection groups) {
        var setId = ushort.Parse(groups["id"].Value);
        var effectId = ushort.Parse(groups["effect"].Value);

        var imcPath = $"chara/equipment/e{setId:0000}/e{setId:0000}.imc";
        var imc = _gameData.GetFile<ImcFile>(imcPath)!;

        var variants = new List<(EquipSlot, byte)>();
        for (var partIdx = 0; partIdx < imc.GetParts().Length; partIdx++) {
            var part = imc.GetPart(partIdx);

            var slot = partIdx switch {
                0 => EquipSlot.Head,
                1 => EquipSlot.Body,
                2 => EquipSlot.Hands,
                3 => EquipSlot.Legs,
                4 => EquipSlot.Feet,
                _ => throw new ArgumentOutOfRangeException(),
            };

            for (var i = 0; i < part.Variants.Length; i++) {
                var info = part.Variants[i];

                if (info.VfxId == effectId) {
                    variants.Add((slot, (byte) (i + 1)));
                }
            }
        }

        return variants.Select(variant => GameObjectInfo.EquipmentVfx(fileType, setId, effectId, variant.Item1, variant.Item2));
    }

    private static IEnumerable<GameObjectInfo> HandleEquipment(FileType fileType, GroupCollection groups) {
        if (fileType == FileType.Vfx) {
            return HandleEquipmentVfx(fileType, groups);
        }

        var setId = ushort.Parse(groups["id"].Value);
        if (fileType == FileType.Imc) {
            return new[] { GameObjectInfo.Equipment(fileType, setId) };
        }

        var gr = Names.GenderRaceFromCode(groups["race"].Value);
        var slot = Names.SuffixToEquipSlot[groups["slot"].Value];
        if (fileType == FileType.Model) {
            return new[] { GameObjectInfo.Equipment(fileType, setId, gr, slot) };
        }

        var variant = byte.Parse(groups["variant"].Value);
        return new[] { GameObjectInfo.Equipment(fileType, setId, gr, slot, variant) };
    }

    private static IEnumerable<GameObjectInfo> HandleWeaponVfx(FileType fileType, ushort setId, ushort weaponId, GroupCollection groups) {
        var effect = ushort.Parse(groups["effect"].Value);

        var imcPath = $"chara/weapon/w{setId:0000}/obj/body/b{weaponId:0000}/b{weaponId:0000}.imc";
        var imc = _gameData.GetFile<ImcFile>(imcPath);
        if (imc == null) {
            Console.WriteLine($"[WARN] missing imc file {imcPath}");
            return Array.Empty<GameObjectInfo>();
        }

        var part = imc.GetPart(0);

        var infos = new List<GameObjectInfo>();

        for (var i = 0; i < part.Variants.Length; i++) {
            var variant = part.Variants[i];
            if (variant.VfxId != effect) {
                continue;
            }

            infos.Add(GameObjectInfo.Weapon(fileType, setId, weaponId, (byte) (i + 1)));
        }

        return infos;
    }

    private static IEnumerable<GameObjectInfo> HandleWeapon(FileType fileType, GroupCollection groups) {
        var weaponId = ushort.Parse(groups["weapon"].Value);
        var setId = ushort.Parse(groups["id"].Value);
        if (fileType is FileType.Imc or FileType.Model or FileType.Skeleton) {
            return new[] { GameObjectInfo.Weapon(fileType, setId, weaponId) };
        }

        if (fileType == FileType.Vfx) {
            return HandleWeaponVfx(fileType, setId, weaponId, groups);
        }

        var variant = byte.Parse(groups["variant"].Value);
        return new[] { GameObjectInfo.Weapon(fileType, setId, weaponId, variant) };
    }

    private static IEnumerable<GameObjectInfo> HandleMonsterVfx(FileType fileType, ushort monsterId, ushort bodyId, GroupCollection groups) {
        var effect = ushort.Parse(groups["effect"].Value);

        var imcPath = $"chara/monster/m{monsterId:0000}/obj/body/b{bodyId:0000}/b{bodyId:0000}.imc";
        var imc = _gameData.GetFile<ImcFile>(imcPath)!;

        var infos = new List<GameObjectInfo>();

        foreach (var part in imc.GetParts()) {
            for (var i = 0; i < part.Variants.Length; i++) {
                var variant = part.Variants[i];
                if (variant.VfxId != effect) {
                    continue;
                }

                infos.Add(GameObjectInfo.Monster(fileType, monsterId, bodyId, (byte) (i + 1)));
            }
        }

        return infos;
    }

    private static IEnumerable<GameObjectInfo> HandleMonster(FileType fileType, GroupCollection groups) {
        var monsterId = ushort.Parse(groups["monster"].Value);
        var bodyId = ushort.Parse(groups["id"].Value);
        if (fileType is FileType.Imc or FileType.Model or FileType.Skeleton) {
            return new[] { GameObjectInfo.Monster(fileType, monsterId, bodyId) };
        }

        if (fileType == FileType.Vfx) {
            return HandleMonsterVfx(fileType, monsterId, bodyId, groups);
        }

        var variant = byte.Parse(groups["variant"].Value);
        return new[] { GameObjectInfo.Monster(fileType, monsterId, bodyId, variant) };
    }

    private static IEnumerable<GameObjectInfo> HandleDemiHumanVfx(FileType fileType, ushort demiHumanId, ushort equipId, GroupCollection groups) {
        var effect = ushort.Parse(groups["effect"].Value);

        var imcPath = $"chara/demihuman/d{demiHumanId:0000}/obj/equipment/e{equipId:0000}/e{equipId:0000}.imc";
        var imc = _gameData.GetFile<ImcFile>(imcPath)!;

        var infos = new List<GameObjectInfo>();

        foreach (var part in imc.GetParts()) {
            for (var i = 0; i < part.Variants.Length; i++) {
                var variant = part.Variants[i];
                if (variant.VfxId != effect) {
                    continue;
                }

                infos.Add(GameObjectInfo.DemiHuman(fileType, demiHumanId, equipId, variant: (byte) (i + 1)));
            }
        }

        return infos;
    }

    private static IEnumerable<GameObjectInfo> HandleDemiHuman(FileType fileType, GroupCollection groups) {
        var demiHumanId = ushort.Parse(groups["id"].Value);
        var equipId = ushort.Parse(groups["equip"].Value);
        if (fileType is FileType.Imc or FileType.Skeleton) {
            return new[] { GameObjectInfo.DemiHuman(fileType, demiHumanId, equipId) };
        }

        if (fileType == FileType.Vfx) {
            return HandleDemiHumanVfx(fileType, demiHumanId, equipId, groups);
        }

        var slot = Names.SuffixToEquipSlot[groups["slot"].Value];
        if (fileType == FileType.Model) {
            return new[] { GameObjectInfo.DemiHuman(fileType, demiHumanId, equipId, slot) };
        }

        var variant = byte.Parse(groups["variant"].Value);
        return new[] { GameObjectInfo.DemiHuman(fileType, demiHumanId, equipId, slot, variant) };
    }

    private static GameObjectInfo HandleCustomization(FileType fileType, GroupCollection groups) {
        if (groups["catchlight"].Success) {
            return GameObjectInfo.Customization(fileType, CustomizationType.Iris);
        }

        if (groups["skin"].Success) {
            return GameObjectInfo.Customization(fileType, CustomizationType.Skin);
        }

        var id = ushort.Parse(groups["id"].Value);
        if (groups["location"].Success) {
            var tmpType = groups["location"].Value switch {
                "face" => CustomizationType.DecalFace,
                "equip" => CustomizationType.DecalEquip,
                _ => CustomizationType.Unknown,
            };
            return GameObjectInfo.Customization(fileType, tmpType, id);
        }

        var gr = Names.GenderRaceFromCode(groups["race"].Value);
        if (fileType == FileType.Skeleton) {
            var slot = Names.SuffixToCustomizationType[groups["type"].Value];
            return GameObjectInfo.Customization(fileType, slot, id, gr);
        }

        var bodySlot = Names.StringToBodySlot[groups["type"].Value];
        var type = groups["slot"].Success
            ? Names.SuffixToCustomizationType[groups["slot"].Value]
            : CustomizationType.Skin;
        if (fileType == FileType.Material) {
            var variant = groups["variant"].Success ? byte.Parse(groups["variant"].Value) : (byte) 0;
            return GameObjectInfo.Customization(fileType, type, id, gr, bodySlot, variant);
        }

        return GameObjectInfo.Customization(fileType, type, id, gr, bodySlot);
    }

    private static GameObjectInfo HandleIcon(FileType fileType, GroupCollection groups) {
        var hq = groups["hq"].Success;
        var hr = groups["hr"].Success;
        var id = uint.Parse(groups["id"].Value);
        if (!groups["lang"].Success) {
            return GameObjectInfo.Icon(fileType, id, hq, hr);
        }

        var language = groups["lang"].Value switch {
            "en" => ClientLanguage.English,
            "ja" => ClientLanguage.Japanese,
            "de" => ClientLanguage.German,
            "fr" => ClientLanguage.French,
            _ => ClientLanguage.English,
        };
        return GameObjectInfo.Icon(fileType, id, hq, hr, language);
    }

    private static GameObjectInfo HandleMap(FileType fileType, GroupCollection groups) {
        var map = Encoding.ASCII.GetBytes(groups["id"].Value);
        var variant = byte.Parse(groups["variant"].Value);
        if (groups["suffix"].Success) {
            var suffix = Encoding.ASCII.GetBytes(groups["suffix"].Value)[0];
            return GameObjectInfo.Map(fileType, map[0], map[1], map[2], map[3], variant, suffix);
        }

        return GameObjectInfo.Map(fileType, map[0], map[1], map[2], map[3], variant);
    }

    public IEnumerable<GameObjectInfo> GetFileInfo(string path) {
        var (fileType, objectType, match) = this.ParseGamePath(path);
        if (match is not { Success: true }) {
            return new[] { new GameObjectInfo { FileType = fileType, ObjectType = objectType } };
        }

        try {
            var groups = match.Groups;
            switch (objectType) {
                case ObjectType.Accessory: return HandleEquipment(fileType, groups);
                case ObjectType.Equipment: return HandleEquipment(fileType, groups);
                case ObjectType.Weapon: return HandleWeapon(fileType, groups);
                case ObjectType.Map: return new[] { HandleMap(fileType, groups) };
                case ObjectType.Monster: return HandleMonster(fileType, groups);
                case ObjectType.DemiHuman: return HandleDemiHuman(fileType, groups);
                case ObjectType.Character: return new[] { HandleCustomization(fileType, groups) };
                case ObjectType.Icon: return new[] { HandleIcon(fileType, groups) };
            }
        } catch (Exception e) {
            Console.WriteLine($"Could not parse {path}:\n{e}");
        }

        return new[] { new GameObjectInfo { FileType = fileType, ObjectType = objectType } };
    }

    private readonly Regex _vfxRegexTmb = new(@"chara/action/(?'key'[^\s]+?)\.tmb");
    private readonly Regex _vfxRegexPap = new(@"chara/human/c0101/animation/a0001/[^\s]+?/(?'key'[^\s]+?)\.pap");

    public string VfxToKey(string path) {
        var match = this._vfxRegexTmb.Match(path);
        if (match.Success) {
            return match.Groups["key"].Value.ToLowerInvariant();
        }

        match = this._vfxRegexPap.Match(path);
        return match.Success ? match.Groups["key"].Value.ToLowerInvariant() : string.Empty;
    }
}
