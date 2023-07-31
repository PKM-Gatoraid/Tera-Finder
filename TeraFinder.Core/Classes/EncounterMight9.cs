using PKHeX.Core;
using static System.Buffers.Binary.BinaryPrimitives;

namespace TeraFinder.Core;

//Extension of https://github.com/kwsch/PKHeX/blob/master/PKHeX.Core/Legality/Encounters/EncounterStatic/EncounterMight9.cs
public sealed record EncounterMight9 : IEncounterable, IEncounterConvertible<PK9>, ITeraRaid9, IMoveset, IFlawlessIVCount, IFixedGender
{
    public int Generation => 9;
    int ILocation.Location => Location;
    public const ushort Location = Locations.TeraCavern9;
    public EntityContext Context => EntityContext.Gen9;
    public GameVersion Version => GameVersion.SV;
    public bool IsDistribution => Index != 0;
    public Ball FixedBall => Ball.None;
    public bool EggEncounter => false;
    public bool IsShiny => Shiny == Shiny.Always;
    public int EggLocation => 0;

    public required Moveset Moves { get; init; }
    public required IndividualValueSet IVs { get; init; }
    public required ushort Species { get; init; }
    public required byte Form { get; init; }
    public required byte Level { get; init; }
    public required sbyte Gender { get; init; }
    public required byte FlawlessIVCount { get; init; }
    public required AbilityPermission Ability { get; init; }
    public required Shiny Shiny { get; init; }
    public required Nature Nature { get; init; }
    public required GemType TeraType { get; init; }
    public required byte Index { get; init; }
    public required byte Stars { get; init; }
    public required byte RandRate { get; init; } // weight chance of this encounter

    //TeraFinder Serialization
    public uint Identifier { get; private init; }
    public ulong FixedRewardHash { get; private init; }
    public ulong LotteryRewardHash { get; private init; }
    public int Item { get; private init; }

    /// <summary> Indicates how the <see cref="Scale"/> value is used, if at all. </summary>
    public SizeType9 ScaleType { get; private init; }
    /// <summary>  Used only for <see cref="ScaleType"/> == <see cref="SizeType9.VALUE"/> </summary>
    public byte Scale { get; private init; }

    public ushort RandRate0MinScarlet { get; private init; }
    public ushort RandRate0MinViolet { get; private init; }
    public ushort RandRate0TotalScarlet { get; private init; }
    public ushort RandRate0TotalViolet { get; private init; }

    public ushort RandRate1MinScarlet { get; private init; }
    public ushort RandRate1MinViolet { get; private init; }
    public ushort RandRate1TotalScarlet { get; private init; }
    public ushort RandRate1TotalViolet { get; private init; }

    public ushort RandRate2MinScarlet { get; private init; }
    public ushort RandRate2MinViolet { get; private init; }
    public ushort RandRate2TotalScarlet { get; private init; }
    public ushort RandRate2TotalViolet { get; private init; }

    public ushort RandRate3MinScarlet { get; private init; }
    public ushort RandRate3MinViolet { get; private init; }
    public ushort RandRate3TotalScarlet { get; private init; }
    public ushort RandRate3TotalViolet { get; private init; }

    public string Name => "7-Star Tera Raid Encounter";
    public string LongName => Name;
    public byte LevelMin => Level;
    public byte LevelMax => Level;

    public ushort GetRandRateTotalScarlet(int stage) => stage switch
    {
        0 => RandRate0TotalScarlet,
        1 => RandRate1TotalScarlet,
        2 => RandRate2TotalScarlet,
        3 => RandRate3TotalScarlet,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    public ushort GetRandRateTotalViolet(int stage) => stage switch
    {
        0 => RandRate0TotalViolet,
        1 => RandRate1TotalViolet,
        2 => RandRate2TotalViolet,
        3 => RandRate3TotalViolet,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    public ushort GetRandRateMinScarlet(int stage) => stage switch
    {
        0 => RandRate0MinScarlet,
        1 => RandRate1MinScarlet,
        2 => RandRate2MinScarlet,
        3 => RandRate3MinScarlet,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    public ushort GetRandRateMinViolet(int stage) => stage switch
    {
        0 => RandRate0MinViolet,
        1 => RandRate1MinViolet,
        2 => RandRate2MinViolet,
        3 => RandRate3MinViolet,
        _ => throw new ArgumentOutOfRangeException(nameof(stage)),
    };

    private const int StageCount = 4;
    private const int StageNone = -1;

    public bool CanBeEncountered(uint seed) => GetProgressMaximum(seed) != StageNone;

    public int ProgressStageMin
    {
        get
        {
            for (int stage = 0; stage < StageCount; stage++)
            {
                if (GetRandRateTotalScarlet(stage) != 0 || GetRandRateTotalViolet(stage) != 0)
                    return stage;
            }
            return StageNone;
        }
    }

    public int ProgressStageMax
    {
        get
        {
            for (int stage = StageCount - 1; stage >= 0; stage--)
            {
                if (GetRandRateTotalScarlet(stage) != 0 || GetRandRateTotalViolet(stage) != 0)
                    return stage;
            }
            return StageNone;
        }
    }

    public int GetProgressMaximum(uint seed)
    {
        // We loop from the highest progress, since that is where the majority of samples will be from.
        for (int i = StageCount - 1; i >= 0; i--)
        {
            if (GetIsPossibleSlot(seed, i))
                return i;
        }
        return StageNone;
    }

    private bool GetIsPossibleSlot(uint seed, int stage)
    {
        var totalScarlet = GetRandRateTotalScarlet(stage);
        if (totalScarlet != 0)
        {
            var rand = new Xoroshiro128Plus(seed);
            _ = rand.NextInt(100);
            var val = rand.NextInt(totalScarlet);
            var min = GetRandRateMinScarlet(stage);
            if ((uint)((int)val - min) < RandRate)
                return true;
        }

        var totalViolet = GetRandRateTotalViolet(stage);
        if (totalViolet != 0)
        {
            var rand = new Xoroshiro128Plus(seed);
            _ = rand.NextInt(100);
            var val = rand.NextInt(totalViolet);
            var min = GetRandRateMinViolet(stage);
            if ((uint)((int)val - min) < RandRate)
                return true;
        }
        return false;
    }

    public static EncounterMight9[] GetArray(ReadOnlySpan<byte> data)
    {
        var count = data.Length / SerializedSize;
        var result = new EncounterMight9[count];
        for (int i = 0; i < result.Length; i++)
            result[i] = ReadEncounter(data.Slice(i * SerializedSize, SerializedSize));
        return result;
    }

    private const int SerializedSize = WeightStart + (sizeof(ushort) * 2 * 2 * 4) + 10 + (sizeof(uint) * 2) + (sizeof(ulong) * 2);
    private const int WeightStart = 0x14;

    private static EncounterMight9 ReadEncounter(ReadOnlySpan<byte> data) => new()
    {
        Species = ReadUInt16LittleEndian(data),
        Form = data[0x02],
        Gender = (sbyte)(data[0x03] - 1),
        Ability = GetAbility(data[0x04]),
        FlawlessIVCount = data[5],
        Shiny = data[0x06] switch { 0 => Shiny.Random, 1 => Shiny.Never, 2 => Shiny.Always, _ => throw new ArgumentOutOfRangeException(nameof(data)) },
        Level = data[0x07],
        Moves = new Moveset(
            ReadUInt16LittleEndian(data[0x08..]),
            ReadUInt16LittleEndian(data[0x0A..]),
            ReadUInt16LittleEndian(data[0x0C..]),
            ReadUInt16LittleEndian(data[0x0E..])),
        TeraType = (GemType)data[0x10],
        Index = data[0x11],
        Stars = data[0x12],
        RandRate = data[0x13],

        RandRate0MinScarlet = ReadUInt16LittleEndian(data[WeightStart..]),
        RandRate0MinViolet = ReadUInt16LittleEndian(data[(WeightStart + sizeof(ushort))..]),
        RandRate0TotalScarlet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 2))..]),
        RandRate0TotalViolet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 3))..]),

        RandRate1MinScarlet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 4))..]),
        RandRate1MinViolet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 5))..]),
        RandRate1TotalScarlet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 6))..]),
        RandRate1TotalViolet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 7))..]),

        RandRate2MinScarlet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 8))..]),
        RandRate2MinViolet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 9))..]),
        RandRate2TotalScarlet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 10))..]),
        RandRate2TotalViolet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 11))..]),

        RandRate3MinScarlet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 12))..]),
        RandRate3MinViolet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 13))..]),
        RandRate3TotalScarlet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 14))..]),
        RandRate3TotalViolet = ReadUInt16LittleEndian(data[(WeightStart + (sizeof(ushort) * 15))..]),

        Nature = (Nature)data[0x34],
        IVs = new IndividualValueSet((sbyte)data[0x35], (sbyte)data[0x36], (sbyte)data[0x37], (sbyte)data[0x38], (sbyte)data[0x39], (sbyte)data[0x3A], (IndividualValueSetType)data[0x3B]),
        ScaleType = (SizeType9)data[0x3C],
        Scale = data[0x3D],

        Identifier = ReadUInt32LittleEndian(data[0x3E..]),
        FixedRewardHash = ReadUInt64LittleEndian(data[0x42..]),
        LotteryRewardHash = ReadUInt64LittleEndian(data[0x4A..]),
        Item = (int)ReadUInt32LittleEndian(data[0x52..]),
    };

    private static AbilityPermission GetAbility(byte b) => b switch
    {
        0 => AbilityPermission.Any12,
        1 => AbilityPermission.Any12H,
        2 => AbilityPermission.OnlyFirst,
        3 => AbilityPermission.OnlySecond,
        4 => AbilityPermission.OnlyHidden,
        _ => throw new ArgumentOutOfRangeException(nameof(b), b, null),
    };

    private byte GetGender() => Gender switch
    {
        0 => PersonalInfo.RatioMagicMale,
        1 => PersonalInfo.RatioMagicFemale,
        2 => PersonalInfo.RatioMagicGenderless,
        _ => PersonalTable.SV.GetFormEntry(Species, Form).Gender,
    };

    #region Generating
    PKM IEncounterConvertible.ConvertToPKM(ITrainerInfo tr, EncounterCriteria criteria) => ConvertToPKM(tr, criteria);
    PKM IEncounterConvertible.ConvertToPKM(ITrainerInfo tr) => ConvertToPKM(tr);
    public PK9 ConvertToPKM(ITrainerInfo tr) => ConvertToPKM(tr, EncounterCriteria.Unrestricted);
    public PK9 ConvertToPKM(ITrainerInfo tr, EncounterCriteria criteria)
    {
        int lang = (int)Language.GetSafeLanguage(Generation, (LanguageID)tr.Language);
        var version = this.GetCompatibleVersion((GameVersion)tr.Game);
        var pk = new PK9
        {
            Language = lang,
            Species = Species,
            Form = Form,
            CurrentLevel = LevelMin,
            OT_Friendship = PersonalTable.SV[Species, Form].BaseFriendship,
            Met_Location = Location,
            Met_Level = LevelMin,
            Version = (int)version,
            Ball = (byte)Ball.Poke,

            Nickname = SpeciesName.GetSpeciesNameGeneration(Species, lang, Generation),
            Obedience_Level = LevelMin,
            RibbonMarkMightiest = true,
        };
        SetPINGA(pk, criteria);
        pk.SetMoves(Moves);

        pk.ResetPartyStats();
        return pk;
    }

    private void SetPINGA(PK9 pk, EncounterCriteria criteria)
    {
        const byte rollCount = 1;
        const byte undefinedSize = 0;
        byte gender = GetGender();
        var param = new GenerateParam9(Species, gender, FlawlessIVCount, rollCount,
            undefinedSize, undefinedSize, ScaleType, Scale,
            Ability, Shiny, Nature, IVs);

        var init = Util.Rand.Rand64();
        var success = this.TryApply32(pk, init, param, criteria);
        if (!success)
            this.TryApply32(pk, init, param, EncounterCriteria.Unrestricted);
    }
    #endregion
}