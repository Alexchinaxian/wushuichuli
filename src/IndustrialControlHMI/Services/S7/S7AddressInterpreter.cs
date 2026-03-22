using System;
using System.Globalization;
using System.Text.RegularExpressions;
using IndustrialControlHMI.Models.Flowchart;
using S7.Net;
using S7.Net.Types;

namespace IndustrialControlHMI.Services.S7;

/// <summary>
/// 将 MCGS 风格地址解析为 S7.Net 读操作（S7-200 / SMART 系列：V 区映射为 DB1 字节偏移）。
/// </summary>
internal static class S7AddressInterpreter
{
    private const int VDbNumber = 1;

    private static readonly Regex IqmBit = new(@"^([IQM])(\d+)\.(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Vdf = new(@"^VDF(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Vwub = new(@"^VWUB(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Vbub = new(@"^VBUB(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool TryRead(Plc plc, PlcPointMapping mapping, out object? value, out string? error)
    {
        value = null;
        error = null;
        var addr = mapping.RegisterAddress?.Trim() ?? string.Empty;

        try
        {
            var mBit = IqmBit.Match(addr);
            if (mBit.Success)
            {
                var area = char.ToUpperInvariant(mBit.Groups[1].Value[0]);
                var byteOffset = int.Parse(mBit.Groups[2].Value, CultureInfo.InvariantCulture);
                var bitIndex = int.Parse(mBit.Groups[3].Value, CultureInfo.InvariantCulture);

                DataType dt = area switch
                {
                    'I' => DataType.Input,
                    'Q' => DataType.Output,
                    'M' => DataType.Memory,
                    _ => DataType.Memory
                };

                // S7.Net: Read(DataType, db, startByte, VarType, count, bitAdr)
                var raw = plc.Read(dt, 0, byteOffset, VarType.Bit, 1, (byte)bitIndex);
                value = raw is bool b ? b : Convert.ToBoolean(raw);
                return true;
            }

            var mVdf = Vdf.Match(addr);
            if (mVdf.Success)
            {
                var byteOffset = int.Parse(mVdf.Groups[1].Value, CultureInfo.InvariantCulture);
                var raw = plc.Read(DataType.DataBlock, VDbNumber, byteOffset, VarType.Real, 1);
                value = Convert.ToSingle(raw);
                return true;
            }

            var mVw = Vwub.Match(addr);
            if (mVw.Success)
            {
                var byteOffset = int.Parse(mVw.Groups[1].Value, CultureInfo.InvariantCulture);
                var raw = plc.Read(DataType.DataBlock, VDbNumber, byteOffset, VarType.Word, 1);
                value = Convert.ToUInt16(raw);
                return true;
            }

            var mVb = Vbub.Match(addr);
            if (mVb.Success)
            {
                var byteOffset = int.Parse(mVb.Groups[1].Value, CultureInfo.InvariantCulture);
                var raw = plc.Read(DataType.DataBlock, VDbNumber, byteOffset, VarType.Byte, 1);
                value = Convert.ToByte(raw);
                return true;
            }

            error = $"不支持的地址格式: {addr}";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryWriteBit(Plc plc, char area, int byteOffset, int bitIndex, bool value, out string? error)
    {
        error = null;
        try
        {
            DataType dt = char.ToUpperInvariant(area) switch
            {
                'Q' => DataType.Output,
                'M' => DataType.Memory,
                _ => DataType.Memory
            };
            plc.WriteBit(dt, 0, byteOffset, bitIndex, value);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
