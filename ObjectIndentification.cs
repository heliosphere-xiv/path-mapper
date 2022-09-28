using System.ComponentModel;
using System.Text;
using Dalamud.Utility;
using Lumina;
using Lumina.Data.Parsing;
using Lumina.Excel.GeneratedSheets;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace PathMapper;

internal class ObjectIdentification {
    private readonly List<(ulong, HashSet<Item>)> _weapons;
    private readonly List<(ulong, HashSet<Item>)> _equipment;
    private readonly Dictionary<string, HashSet<Action>> _actions;
    private readonly GamePathParser _parser;
    private readonly GameData _gameData;
    private readonly BNpcContainer _bnpcs;

    private static bool Add(IDictionary<ulong, HashSet<Item>> dict, ulong key, Item item) {
        if (dict.TryGetValue(key, out var list)) {
            return list.Add(item);
        }

        dict[key] = new HashSet<Item> { item };
        return true;
    }

    private static ulong EquipmentKey(Item i) {
        var model = (ulong) ((Quad) i.ModelMain).A;
        var variant = (ulong) ((Quad) i.ModelMain).B;
        var slot = (ulong) ((EquipSlot) i.EquipSlotCategory.Row).ToSlot();
        return (model << 32) | (slot << 16) | variant;
    }

    private static ulong WeaponKey(Item i, bool offhand) {
        var quad = offhand ? (Quad) i.ModelSub : (Quad) i.ModelMain;
        var model = (ulong) quad.A;
        var type = (ulong) quad.B;
        var variant = (ulong) quad.C;

        return (model << 32) | (type << 16) | variant;
    }

    private void AddAction(string key, Action action) {
        if (key.Length == 0) {
            return;
        }

        key = key.ToLowerInvariant();
        if (this._actions.TryGetValue(key, out var actions)) {
            actions.Add(action);
        } else {
            this._actions[key] = new HashSet<Action> { action };
        }
    }

    public ObjectIdentification(GameData dataManager, GamePathParser parser, BNpcContainer bnpcs) {
        this._gameData = dataManager;
        this._parser = parser;
        this._bnpcs = bnpcs;
        var items = dataManager.GetExcelSheet<Item>()!;
        SortedList<ulong, HashSet<Item>> weapons = new();
        SortedList<ulong, HashSet<Item>> equipment = new();
        foreach (var item in items) {
            switch ((EquipSlot) item.EquipSlotCategory.Row) {
                case EquipSlot.MainHand:
                case EquipSlot.OffHand:
                case EquipSlot.BothHand:
                    if (item.ModelMain != 0) {
                        Add(weapons, WeaponKey(item, false), item);
                    }

                    if (item.ModelSub != 0) {
                        Add(weapons, WeaponKey(item, true), item);
                    }

                    break;
                // Accessories
                case EquipSlot.RFinger:
                case EquipSlot.Wrists:
                case EquipSlot.Ears:
                case EquipSlot.Neck:
                    Add(equipment, EquipmentKey(item), item);
                    break;
                // Equipment
                case EquipSlot.Head:
                case EquipSlot.Body:
                case EquipSlot.Hands:
                case EquipSlot.Legs:
                case EquipSlot.Feet:
                case EquipSlot.BodyHands:
                case EquipSlot.BodyHandsLegsFeet:
                case EquipSlot.BodyLegsFeet:
                case EquipSlot.FullBody:
                case EquipSlot.HeadBody:
                case EquipSlot.LegsFeet:
                    Add(equipment, EquipmentKey(item), item);
                    break;
                default: continue;
            }
        }

        this._actions = new Dictionary<string, HashSet<Action>>();
        foreach (var action in dataManager.GetExcelSheet<Action>()!
                     .Where(a => a.Name.ToString().Any())) {
            var startKey = action.AnimationStart?.Value?.Name?.Value?.Key.ToString() ?? string.Empty;
            var endKey = action.AnimationEnd?.Value?.Key.ToString() ?? string.Empty;
            var hitKey = action.ActionTimelineHit?.Value?.Key.ToString() ?? string.Empty;
            this.AddAction(startKey, action);
            this.AddAction(endKey, action);
            this.AddAction(hitKey, action);
        }

        this._weapons = weapons.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        this._equipment = equipment.Select(kvp => (kvp.Key, kvp.Value)).ToList();
    }

    private class Comparer : IComparer<(ulong, HashSet<Item>)> {
        public int Compare((ulong, HashSet<Item>) x, (ulong, HashSet<Item>) y) => x.Item1.CompareTo(y.Item1);
    }

    private static (int, int) FindIndexRange(List<(ulong, HashSet<Item>)> list, ulong key, ulong mask) {
        var maskedKey = key & mask;
        var idx = list.BinarySearch(0, list.Count, (key, null!), new Comparer());
        if (idx < 0) {
            if (~idx == list.Count || maskedKey != (list[~idx].Item1 & mask)) {
                return (-1, -1);
            }

            idx = ~idx;
        }

        var endIdx = idx + 1;
        while (endIdx < list.Count && maskedKey == (list[endIdx].Item1 & mask)) {
            ++endIdx;
        }

        return (idx, endIdx);
    }

    private void FindEquipment(IDictionary<string, object?> set, GameObjectInfo info) {
        var key = (ulong) info.PrimaryId << 32;
        var mask = 0xFFFF00000000ul;
        if (info.EquipSlot != EquipSlot.Unknown) {
            key |= (ulong) info.EquipSlot.ToSlot() << 16;
            mask |= 0xFFFF0000;
        }

        if (info.Variant != 0) {
            key |= info.Variant;
            mask |= 0xFFFF;
        }

        var (start, end) = FindIndexRange(this._equipment, key, mask);
        if (start == -1) {
            return;
        }

        for (; start < end; ++start) {
            foreach (var item in this._equipment[start].Item2) {
                set[item.Name.ToString()] = item;
            }
        }
    }

    private void FindWeapon(IDictionary<string, object?> set, GameObjectInfo info) {
        var key = (ulong) info.PrimaryId << 32;
        var mask = 0xFFFF00000000ul;
        if (info.SecondaryId != 0) {
            key |= (ulong) info.SecondaryId << 16;
            mask |= 0xFFFF0000;
        }

        if (info.Variant != 0) {
            key |= info.Variant;
            mask |= 0xFFFF;
        }

        var (start, end) = FindIndexRange(this._weapons, key, mask);
        if (start == -1) {
            return;
        }

        for (; start < end; ++start) {
            foreach (var item in this._weapons[start].Item2) {
                set[item.Name.ToString()] = item;
            }
        }
    }

    private static void AddCounterString(IDictionary<string, object?> set, string data) {
        if (set.TryGetValue(data, out var obj) && obj is int counter) {
            set[data] = counter + 1;
        } else {
            set[data] = 1;
        }
    }

    private readonly Dictionary<ushort, HashSet<string>> _cachedBNpcs = new();
    private readonly Dictionary<ushort, HashSet<string>> _cachedMonsters = new();

    private void IdentifyParsed(IDictionary<string, object?> set, GameObjectInfo info) {
        switch (info.ObjectType) {
            case ObjectType.Unknown:
                switch (info.FileType) {
                    case FileType.Sound:
                        AddCounterString(set, FileType.Sound.ToString());
                        break;
                    case FileType.Animation:
                    case FileType.Pap:
                        AddCounterString(set, FileType.Animation.ToString());
                        break;
                    case FileType.Shader:
                        AddCounterString(set, FileType.Shader.ToString());
                        break;
                }

                break;
            case ObjectType.LoadingScreen:
            case ObjectType.Interface:
            case ObjectType.Vfx:
            case ObjectType.World:
            case ObjectType.Housing:
            case ObjectType.Font:
                AddCounterString(set, info.ObjectType.ToString());
                break;
            case ObjectType.Map: {
                var id = string.Join("", new[] {
                    (char) info.MapC1,
                    (char) info.MapC2,
                    (char) info.MapC3,
                    (char) info.MapC4,
                });
                var realId = $"{id}/{info.Variant:00}";
                var names = this._gameData.GetExcelSheet<Map>()!
                    .Where(row => row.Id == realId)
                    .Select(row => (row.PlaceNameRegion.Value!.Name.ToDalamudString().TextValue.Trim(), row.PlaceName.Value!.Name.ToDalamudString().TextValue.Trim(), row.PlaceNameSub.Value!.Name.ToDalamudString().TextValue.Trim()))
                    .Select(pn => {
                        var sb = new StringBuilder();
                        if (!string.IsNullOrWhiteSpace(pn.Item1)) {
                            sb.Append(pn.Item1);
                        }

                        if (!string.IsNullOrWhiteSpace(pn.Item2)) {
                            if (sb.Length > 0) {
                                sb.Append(" - ");
                            }

                            sb.Append(pn.Item2);
                        }

                        if (!string.IsNullOrWhiteSpace(pn.Item3) && pn.Item2 != pn.Item3) {
                            var empty = sb.Length == 0;
                            if (!empty) {
                                sb.Append(" (");
                            }

                            sb.Append(pn.Item3);

                            if (!empty) {
                                sb.Append(')');
                            }
                        }

                        return $"Map: {sb}";
                    })
                    .ToHashSet();
                foreach (var name in names) {
                    set[name] = null;
                }

                break;
            }
            case ObjectType.DemiHuman: {
                if (!this._cachedBNpcs.TryGetValue(info.PrimaryId, out var names)) {
                    names = this._gameData.GetExcelSheet<BNpcBase>()!
                        .Where(row => row.ModelChara.Value!.Type == 2 && row.ModelChara.Value.Model == info.PrimaryId)
                        .SelectMany(row => this._bnpcs.bnpc.Where(bnpc => bnpc.bnpcBase == row.RowId))
                        .Select(e => this._gameData.GetExcelSheet<BNpcName>()!.GetRow(e.bnpcName)?.Singular.ToDalamudString().TextValue.Trim())
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Cast<string>()
                        .ToHashSet();
                    this._cachedBNpcs[info.PrimaryId] = names;
                }

                foreach (var name in names) {
                    set[name] = null;
                }

                break;
            }
            case ObjectType.Monster: {
                if (!this._cachedMonsters.TryGetValue(info.PrimaryId, out var names)) {
                    names = this._gameData.GetExcelSheet<ModelChara>()!
                        .Where(row => row.Type == 3 && row.Model == info.PrimaryId)
                        .SelectMany(row => {
                            if (info.PrimaryId < 8000) {
                                return this._gameData.GetExcelSheet<BNpcBase>()!
                                    .Where(b => b.ModelChara.Row == row.RowId)
                                    .SelectMany(b => this._bnpcs.bnpc.Where(bn => bn.bnpcBase == b.RowId))
                                    .Select(e => this._gameData.GetExcelSheet<BNpcName>()!.GetRow(e.bnpcName)?.Singular.ToDalamudString().TextValue.Trim())
                                    .Select(name => $"Battle NPC: {name}");
                            }

                            return this._gameData.GetExcelSheet<Companion>()!
                                .Where(com => com.Model.Row == row.RowId)
                                .Select(com => com.Singular.ToDalamudString().TextValue.Trim())
                                .Select(name => $"Minion: {name}");
                        })
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .ToHashSet();

                    this._cachedMonsters[info.PrimaryId] = names;
                }

                foreach (var name in names) {
                    set[name] = null;
                }

                break;
            }
            case ObjectType.Icon:
                set[$"Icon: {info.IconId}"] = null;
                break;
            case ObjectType.Accessory:
            case ObjectType.Equipment:
                this.FindEquipment(set, info);
                break;
            case ObjectType.Weapon:
                this.FindWeapon(set, info);
                break;
            case ObjectType.Character:
                var (gender, race) = info.GenderRace.Split();
                var raceString = race != ModelRace.Unknown ? race.ToName() + " " : "";
                var genderString = gender != Gender.Unknown ? gender.ToName() + " " : "Player ";
                switch (info.CustomizationType) {
                    case CustomizationType.Skin:
                        set[$"Customization: {raceString}{genderString}Skin Textures"] = null;
                        break;
                    case CustomizationType.DecalFace:
                        set[$"Customization: Face Decal {info.PrimaryId}"] = null;
                        break;
                    case CustomizationType.Iris when race == ModelRace.Unknown:
                        set["Customization: All Eyes (Catchlight)"] = null;
                        break;
                    default: {
                        var customizationString = race == ModelRace.Unknown
                                                  || info.BodySlot == BodySlot.Unknown
                                                  || info.CustomizationType == CustomizationType.Unknown
                            ? "Customization: Unknown"
                            : $"Customization: {race} {gender} {info.BodySlot} ({info.CustomizationType}) {info.PrimaryId}";
                        set[customizationString] = null;
                        break;
                    }
                }

                break;

            default:
                throw new InvalidEnumArgumentException();
        }
    }

    private void IdentifyVfx(IDictionary<string, object?> set, string path) {
        var key = this._parser.VfxToKey(path);
        if (key.Length == 0 || !this._actions.TryGetValue(key, out var actions)) {
            return;
        }

        foreach (var action in actions) {
            set[$"Action: {action.Name}"] = action;
        }
    }

    public void Identify(IDictionary<string, object?> set, string path) {
        if (path.EndsWith(".pap") || path.EndsWith(".tmb")) {
            this.IdentifyVfx(set, path);
        } else {
            var info = this._parser.GetFileInfo(path);
            this.IdentifyParsed(set, info);
        }
    }

    public Dictionary<string, object?> Identify(string path) {
        Dictionary<string, object?> ret = new();
        this.Identify(ret, path);
        return ret;
    }

    public Item? Identify(SetId setId, WeaponType weaponType, ushort variant, EquipSlot slot) {
        switch (slot) {
            case EquipSlot.MainHand:
            case EquipSlot.OffHand: {
                var (begin, _) = FindIndexRange(this._weapons, ((ulong) setId << 32) | ((ulong) weaponType << 16) | variant,
                    0xFFFFFFFFFFFF);
                return begin >= 0 ? this._weapons[begin].Item2.FirstOrDefault() : null;
            }
            default: {
                var (begin, _) = FindIndexRange(this._equipment,
                    ((ulong) setId << 32) | ((ulong) slot.ToSlot() << 16) | variant,
                    0xFFFFFFFFFFFF);
                return begin >= 0 ? this._equipment[begin].Item2.FirstOrDefault() : null;
            }
        }
    }
}
