﻿#region copyright
// KeeYaOtp, a KeePass plugin that generate one-time passwords for Yandex 2FA
// Copyright (C) 2020 norblik
//
// This plugin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This plugin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this plugin. If not, see <https://www.gnu.org/licenses/>.
//
// SPDX-License-Identifier: GPL-3.0-or-later
#endregion

using System;

namespace KeeYaOtp.Core
{
    public static class Base32Encoder
    {
        public const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        public const char PaddingChar = '=';

        private const byte bitsPerByte = 8;
        private const byte bitsPerChar = 5;
        private const byte charsPerGroup = 8;
        private const byte bytesPerGroup = 5;
        private static readonly byte[] paddings = { 0, 1, 3, 4, 6 };

        public static string Encode(byte[] input, bool padding = false)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var groupsCount = (input.Length + (bytesPerGroup - 1)) / bytesPerGroup;

            var lastGroupFakeBytes = groupsCount * bytesPerGroup - input.Length;
            var paddingLength = paddings[lastGroupFakeBytes];
            var outputLength = padding ? groupsCount * charsPerGroup : groupsCount * charsPerGroup - paddingLength;

            var indxOutput = 0;
            var output = new char[outputLength];

            uint accum = 0;
            int bitsInAccum = 0;

            for (int indxInput = 0; indxInput < input.Length; indxInput++)
            {
                accum = accum << bitsPerByte | input[indxInput];
                bitsInAccum += bitsPerByte;

                while (bitsInAccum >= bitsPerChar)
                {
                    bitsInAccum -= bitsPerChar;
                    output[indxOutput++] = Alphabet[(int)(accum >> bitsInAccum & 0x1F)];
                }
            }

            if (bitsInAccum != 0)
            {
                output[indxOutput++] = Alphabet[(int)(accum << bitsPerChar - bitsInAccum & 0x1F)];
            }

            if (padding)
            {
                while (indxOutput < outputLength)
                    output[indxOutput++] = PaddingChar;
            }

            return new string(output);
        }

        public static byte[] Decode(string input, bool checkPadding = false)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length == 0) return new byte[0];

            var paddingIndex = input.Length;
            while (input[paddingIndex - 1] == PaddingChar) paddingIndex--;

            var totalBits = paddingIndex * bitsPerChar;
            var outputLength = totalBits / bitsPerByte;
            if (outputLength == 0) throw new ArgumentException("Too short base32 string", nameof(input));
            var tailBits = totalBits % bitsPerByte;
            if (tailBits >= bitsPerChar) throw new ArgumentException("Redundant number of chars in base32 string", nameof(input));
            //
            var lastvalue = Alphabet.IndexOf(input[paddingIndex - 1]);
            if (lastvalue == -1) throw new ArgumentException("Invalid char in base32 string", nameof(input));
            if (((1 << tailBits) - 1 & lastvalue) != 0) throw new ArgumentException("Non-zero decoded tail bits in trailing char", nameof(input));
            //

            if (checkPadding)
            {
                var groupsCount = (outputLength + (bytesPerGroup - 1)) / bytesPerGroup;
                var lastGroupFakeBytes = groupsCount * bytesPerGroup - outputLength;
                if (input.Length - paddings[lastGroupFakeBytes] != paddingIndex) throw new ArgumentException("Invalid base32 string padding length", nameof(input));
            }

            var indxInput = 0;
            var output = new byte[outputLength];

            ushort accum = 0;
            var bitsInAccum = 0;

            for (int indxOutput = 0; indxOutput < output.Length; indxOutput++)
            {
                while (bitsInAccum < bitsPerByte)
                {
                    var value = Alphabet.IndexOf(input[indxInput++]);
                    if (value != -1)
                    {
                        accum = (ushort)(accum << bitsPerChar | value);
                        bitsInAccum += bitsPerChar;
                    }
                    else
                        throw new ArgumentException("Invalid char in base32 string", nameof(input));
                }

                bitsInAccum -= bitsPerByte;
                output[indxOutput] = (byte)(accum >> bitsInAccum);
            }

            //if ((((1 << bitsInAccum) - 1) & accum) != 0) throw new ArgumentException("Non-zero tail bits in trailing char", nameof(input));

            return output;
        }

    }
}
