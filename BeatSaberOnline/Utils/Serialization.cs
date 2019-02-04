using System;
using System.Linq;
using UnityEngine;

namespace BeatSaberOnline.Utils
{

    static class Serialization
    {
        public static byte[] Combine(params byte[][] arrays)
        {
            byte[] rv = new byte[arrays.Sum(a => a.Length)];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                offset += array.Length;
            }
            return rv;
        }

        private static short compressFloat(float num)
        {
            return (short) (num * 1000);
        }

        private static float decompressFloat(short num)
        {
            return ((float)num / 1000f);
        }

        public static int Vector3Size()
        {
            return (sizeof(short) * 3);
        }

        public static int QuaternionSize()
        {
            return (sizeof(short) * 4);
        }

        public static byte[] ToBytes(Vector3 vect)
        {
            byte[] buff = new byte[sizeof(short) * 3];
            Buffer.BlockCopy(BitConverter.GetBytes(compressFloat(vect.x)), 0, buff, 0 * sizeof(short), sizeof(short));
            Buffer.BlockCopy(BitConverter.GetBytes(compressFloat(vect.y)), 0, buff, 1 * sizeof(short), sizeof(short));
            Buffer.BlockCopy(BitConverter.GetBytes(compressFloat(vect.z)), 0, buff, 2 * sizeof(short), sizeof(short));

            return buff;
        }

        public static byte[] ToBytes(Quaternion vect)
        {
            byte[] buff = new byte[sizeof(short) * 4];
            Buffer.BlockCopy(BitConverter.GetBytes(compressFloat(vect.x)), 0, buff, 0 * sizeof(short), sizeof(short));
            Buffer.BlockCopy(BitConverter.GetBytes(compressFloat(vect.y)), 0, buff, 1 * sizeof(short), sizeof(short));
            Buffer.BlockCopy(BitConverter.GetBytes(compressFloat(vect.z)), 0, buff, 2 * sizeof(short), sizeof(short));
            Buffer.BlockCopy(BitConverter.GetBytes(compressFloat(vect.w)), 0, buff, 3 * sizeof(short), sizeof(short));

            return buff;
        }

        public static Vector3 ToVector3(byte[] data)
        {
            byte[] buff = data;
            Vector3 vect = Vector3.zero;
            vect.x = decompressFloat(BitConverter.ToInt16(buff, 0 * sizeof(short)));
            vect.y = decompressFloat(BitConverter.ToInt16(buff, 1 * sizeof(short)));
            vect.z = decompressFloat(BitConverter.ToInt16(buff, 2 * sizeof(short)));

            return vect;
        }

        public static Quaternion ToQuaternion(byte[] data)
        {
            byte[] buff = data;
            Quaternion vect = Quaternion.identity;
            vect.x = decompressFloat(BitConverter.ToInt16(buff, 0 * sizeof(short)));
            vect.y = decompressFloat(BitConverter.ToInt16(buff, 1 * sizeof(short)));
            vect.z = decompressFloat(BitConverter.ToInt16(buff, 2 * sizeof(short)));
            vect.w = decompressFloat(BitConverter.ToInt16(buff, 3 * sizeof(short)));

            return vect;
        }
    }
}