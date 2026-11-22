using ImportaYa.Application.Models;
using ImportaYa.Domain.Enums;

namespace ImportaYa.Application.Abstractions;

public interface IFileParserFactory
{
    IFileParser GetFor(string extension);

    ImportFormat GetFormat(string extension);
}
