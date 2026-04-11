using Microsoft.International.Converters.PinYinConverter;
using System.Diagnostics.CodeAnalysis;

namespace BetterFabricatorUI;

public static class PinyinHelper
{
    private static readonly Dictionary<char, List<string>> pinyinPrefixesCache = new();

    /// <summary>
    /// 判断模式字符串是否与目标文本的任意连续子串匹配，支持中文字符的拼音前缀模糊匹配。
    /// </summary>
    /// <param name="pattern">要匹配的模式字符串</param>
    /// <param name="text">目标文本</param>
    /// <param name="comparison">字符比较规则，默认忽略大小写</param>
    /// <returns>
    /// 若模式与文本的某一子串匹配，返回 <see langword="true"/>；否则返回 <see langword="false"/>。
    /// 若任一参数为 <see langword="null"/> 或空，返回 <see langword="false"/>。
    /// </returns>
    /// <remarks>
    /// 匹配规则：
    /// <list type="bullet">
    /// <item><description>普通字符按指定的 <paramref name="comparison"/> 逐字比对。</description></item>
    /// <item><description>中文字符允许以其完整拼音或声母前缀（如 "zh", "c", "s" 等）进行匹配。</description></item>
    /// <item><description>模式必须从文本的某一位置开始被完全消耗，方可视为匹配成功。</description></item>
    /// </list>
    /// </remarks>
    public static bool IsMatch(string pattern, string text, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text)) { return false; }

        for (int start = 0; start < text.Length; start++)
        {
            if (TryMatchFromPosition(pattern, text, start, comparison)) { return true; }
        }

        return false;
    }

    private static bool TryMatchFromPosition(string pattern, string text, int start, StringComparison comparison)
    {
        int patIdx = 0;
        int textIdx = start;

        while (patIdx < pattern.Length && textIdx < text.Length)
        {
            if (string.Compare(pattern, patIdx, text, textIdx, length: 1, comparison) == 0)
            {
                patIdx++;
                textIdx++;
                continue;
            }

            if (ChineseCharOptimized.TryGet(text[textIdx], out var cc))
            {
                List<string> pinyinPrefixes = GetPinyinPrefixes(cc);
                string? matched = null;
                foreach (var prefix in pinyinPrefixes)
                {
                    if (patIdx + prefix.Length <= pattern.Length &&
                        string.Compare(pattern, patIdx, prefix, 0, prefix.Length, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        matched = prefix;
                        break;
                    }
                }
                if (matched == null) { return false; }
                patIdx += matched.Length;
                textIdx++;
                continue;
            }

            return false;
        }

        return patIdx == pattern.Length;
    }

    private static List<string> GetPinyinPrefixes(ChineseChar cc)
    {
        if (!pinyinPrefixesCache.TryGetValue(cc.ChineseCharacter, out var pinyinPrefixes))
        {
            var rawPrefixes = new HashSet<string>();

            var pinyins = cc.Pinyins
                .Take(cc.PinyinCount)
                .Select(py => py[..^1])
                .Distinct();

            foreach (var py in pinyins)
            {
                rawPrefixes.Add(py);

                if (TryGetInitialConsonant(py, out string? initial))
                {
                    rawPrefixes.Add(initial);
                    if (initial.Length > 1)
                    {
                        rawPrefixes.Add(py[0..1]);
                    }
                }
            }

            pinyinPrefixes = rawPrefixes.OrderDescending().ToList();
            pinyinPrefixesCache[cc.ChineseCharacter] = pinyinPrefixes;
        }

        return pinyinPrefixes;
    }

    private static bool TryGetInitialConsonant(string pinyin, [NotNullWhen(true)] out string? initial)
    {
        string vowels = "aeiouv";
        int idx = -1;
        while (++idx < pinyin.Length && !vowels.Contains(pinyin[idx], StringComparison.OrdinalIgnoreCase)) ;

        if (idx > 0)
        {
            initial = pinyin[..idx];
            return true;
        }

        initial = null;
        return false;
    }
}

/// <summary>
/// 提供线程安全的中文字符有效性检查与 <see cref="ChineseChar"/> 对象缓存。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ChineseChar.IsValidChar"/> 内部对包含全部中文字符的 <see cref="List{T}"/> 进行线性搜索，
/// 每次调用均需遍历整个集合，时间复杂度为 O(N)。在高频调用场景下，这会引发严重的性能问题。
/// </para>
/// <para>
/// 该类为所有可能的 <see cref="char"/> 值（U+0000 至 U+FFFF）预先分配缓存空间，
/// 并通过双重检查锁（DCL）模式实现按需且线程安全的初始化。首次访问某一字符时执行唯一一次有效性检查，
/// 后续所有查询均直接读取缓存结果，将时间复杂度降低至 O(1)。
/// </para>
/// <para>
/// 源码参考：
/// <see href="https://github.com/netcorepal/PinYinConverterCore/blob/a5330e69eaa0df1152148b092612629e294c9934/PinYinConverterCore/CharDictionary.cs#L60-L64">
/// PinYinConverterCore/CharDictionary.cs (lines 60-64)
/// </see>
/// </para>
/// </remarks>
public static class ChineseCharOptimized
{
    private static readonly object _dcl = new();
    private static readonly bool?[] _validityCache = new bool?[char.MaxValue + 1];
    private static readonly ChineseChar?[] _instanceCache = new ChineseChar?[char.MaxValue + 1];

    public static bool IsValid(char c) => TryGet(c, out _);

    /// <remarks>
    /// <b>Benchmark result:</b>
    /// <code>
    /// Before:
    /// if (ChineseChar.IsValidChar(c)) { var _ = new ChineseChar(c); }
    /// => Mean = 56,411.74 ns, Error = 588.461 ns, StdDev = 389.230 ns, Allocated = 216 B.
    /// 
    /// After :
    /// ChineseCharOptimized.TryGet(c, out var _);
    /// => Mean =     10.76 ns, Error =   0.114 ns, StdDev =   0.075 ns, Allocated =   -  .
    /// </code>
    /// </remarks>
    public static bool TryGet(char c, [NotNullWhen(true)] out ChineseChar? chineseChar)
    {
        bool? valid = _validityCache[c];
        if (!valid.HasValue)
        {
            lock (_dcl)
            {
                valid = _validityCache[c];
                if (!valid.HasValue)
                {
                    valid = ChineseChar.IsValidChar(c);
                    _validityCache[c] = valid;
                }
            }
        }

        if (valid.Value)
        {
            if (_instanceCache[c] is not { } instance)
            {
                lock (_dcl)
                {
                    instance = _instanceCache[c];
                    if (instance is null)
                    {
                        instance = new ChineseChar(c);
                        _instanceCache[c] = instance;
                    }
                }
            }
            chineseChar = instance;
            return true;
        }

        chineseChar = null;
        return false;
    }
}