using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

public class StarNameGenerator
{
    private static readonly Random _random = new Random();
    private static readonly string[] Prefixes = {
        "Ae", "Al", "Am", "Ar", "As", "Az", "Be", "Bi", "Ca", "Ce", "Cy", "Da", "De", "Dra", "Ek", "El", "En", "Epsi", "Fa", "Fe",
        "Ga", "Ge", "Gno", "Ha", "He", "Hy", "I", "Io", "Ir", "Ka", "Ke", "Kry", "La", "Le", "Ly", "Ma", "Me", "Mi", "Na", "Ne",
        "Ny", "O", "Ob", "Oc", "Om", "Pa", "Pe", "Ph", "Qu", "Ra", "Re", "Ri", "Ro", "Sa", "Se", "Si", "Ta", "Te", "Th", "Ti",
        "Ur", "Va", "Ve", "Vi", "Xy", "Za", "Ze", "Zi", "Zet"
    };

    private static readonly string[] Middles = {
        "ba", "be", "bi", "bo", "bu", "ca", "ce", "ci", "co", "cu", "da", "de", "di", "do", "du", "fa", "fe", "fi", "fo", "fu",
        "ga", "ge", "gi", "go", "gu", "la", "le", "li", "lo", "lu", "ma", "me", "mi", "mo", "mu", "na", "ne", "ni", "no", "nu",
        "ra", "re", "ri", "ro", "ru", "sa", "se", "si", "so", "su", "ta", "te", "ti", "to", "tu", "va", "ve", "vi", "vo", "vu",
        "xa", "xe", "xi", "xo", "xu", "za", "ze", "zi", "zo", "zu"
    };

    private static readonly string[] Suffixes = {
        "an", "ar", "as", "ax", "cor", "cus", "dar", "den", "don", "dor", "dus", "en", "er", "es", "eth", "ion", "ior", "is", "ius", "ix",
        "kon", "kor", "kun", "lan", "lar", "len", "lex", "lon", "lus", "lux", "max", "mox", "mus", "nas", "ner", "nes", "nis", "nor", "nus", "on",
        "or", "os", "ox", "rax", "ren", "res", "ris", "ron", "ros", "rus", "tan", "tar", "ten", "ter", "tis", "ton", "tor", "tus", "un", "us",
        "ux", "van", "var", "ven", "ver", "vis", "von", "vor", "vus", "xan", "xar", "xen", "xer", "xis", "xon", "xor", "xus", "zen", "zer"
    };

    private static HashSet<string> _generatedNames = new HashSet<string>();

    public static string GenerateUnique(int maxRetries = 100)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            string newName = BuildRandomName();
            if (_generatedNames.Add(newName))
            {
                return newName;
            }
        }
        return "GenericStar" + _random.Next(1000, 10000);
    }

    private static string BuildRandomName()
    {
        StringBuilder name = new StringBuilder();

        int length = _random.Next(2, 6);

        name.Append(Prefixes[_random.Next(0, Prefixes.Length)]);

        if (length > 2)
        {
            int middleCount = length - 2;
            for (int i = 0; i < middleCount; i++)
            {
                name.Append(Middles[_random.Next(0, Middles.Length)]);
            }
        }

        name.Append(Suffixes[_random.Next(0, Suffixes.Length)]);

        return name.ToString();
    }
}
