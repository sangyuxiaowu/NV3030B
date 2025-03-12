using System.Drawing;

namespace Sang.IoT.NV3030B
{
    /// <summary>
    /// Rgb565 结构表示一个 16 位 RGB 颜色
    /// 其中红色 5 位，绿色 6 位，蓝色 5 位。
    /// </summary>
    public struct Rgb565 : IEquatable<Rgb565>
    {
        private ushort _value;

        public Rgb565(ushort packedValue)
        {
            _value = packedValue;
        }

        public Rgb565(ushort r, ushort g, ushort b)
        {
            _value = 0;
            InitFrom(r, g, b);
        }

        /// <summary>
        /// 获取红色分量 (0-255)
        /// </summary>
        public int R
        {
            get
            {
                int value = (_value & 0xF8);
                return value == 0 ? 0 : value | 0x7;
            }
        }

        /// <summary>
        /// 获取绿色分量 (0-255)
        /// </summary>
        public int G
        {
            get
            {
                int value = ((_value & 0x7E0) >> 5);
                return value == 0 ? 0 : value | 0x3;
            }
        }

        /// <summary>
        /// 获取蓝色分量 (0-255)
        /// </summary>
        public int B
        {
            get
            {
                int value = ((_value >> 11) & 0x1F) << 3;
                return value == 0 ? 0 : value | 0x7;
            }
        }

        private void InitFrom(ushort r, ushort g, ushort b)
        {
            _value = PackRgb(r, g, b);
        }

        /// <summary>
        /// 将 RGB 值打包为 RGB565 格式
        /// </summary>
        private static ushort PackRgb(int r, int g, int b)
        {
            // 获取红色值的高5位
            UInt16 retval = (UInt16)(r >> 3);
            // 左移为绿色值腾出空间
            retval <<= 6;
            // 合并绿色值的高6位
            retval |= (UInt16)(g >> 2);
            // 左移为蓝色值腾出空间
            retval <<= 5;
            // 合并蓝色值的高5位
            retval |= (UInt16)(b >> 3);

            return Swap(retval);
        }

        /// <summary>
        /// 将颜色结构转换为表示 565 格式颜色的 Rgb565 结构
        /// </summary>
        /// <param name="color">要转换的颜色</param>
        public static Rgb565 FromRgba32(Color color)
        {
            return new Rgb565(PackRgb(color.R, color.G, color.B));
        }

        /// <summary>
        /// 从整数 RGB 值创建 Rgb565 实例
        /// </summary>
        public static Rgb565 FromRgba32(int r, int g, int b)
        {
            return new Rgb565(PackRgb(r, g, b));
        }

        /// <summary>
        /// 交换字节顺序 (小端/大端转换)
        /// </summary>
        private static ushort Swap(ushort val)
        {
            return (ushort)((val >> 8) | (val << 8));
        }

        public ushort PackedValue
        {
            get => _value;
            set => _value = value;
        }

        // 添加常用颜色的静态属性
        public static Rgb565 Black => new Rgb565(0);
        public static Rgb565 White => FromRgba32(Color.White);
        public static Rgb565 Red => FromRgba32(Color.Red);
        public static Rgb565 Green => FromRgba32(Color.Green);
        public static Rgb565 Blue => FromRgba32(Color.Blue);
        public static Rgb565 Yellow => FromRgba32(Color.Yellow);
        public static Rgb565 Cyan => FromRgba32(Color.Cyan);
        public static Rgb565 Magenta => FromRgba32(Color.Magenta);
        public static Rgb565 Gray => FromRgba32(Color.Gray);

        public bool Equals(Rgb565 other)
        {
            return _value == other._value;
        }

        public override bool Equals(object? obj)
        {
            return obj is Rgb565 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value;
        }

        public static bool operator ==(Rgb565 left, Rgb565 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Rgb565 left, Rgb565 right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// 如果两个颜色几乎相等，则返回 true
        /// </summary>
        /// <param name="a">第一个颜色</param>
        /// <param name="b">第二个颜色</param>
        /// <param name="delta">允许的差异，以可见位为单位</param>
        /// <returns>颜色是否几乎相等</returns>
        public static bool AlmostEqual(Rgb565 a, Rgb565 b, int delta)
        {
            if (a.PackedValue == b.PackedValue)
            {
                return true;
            }

            if (Math.Abs(a.R - b.R) > (delta << 3))
            {
                return false;
            }

            if (Math.Abs(a.G - b.G) > (delta << 2))
            {
                return false;
            }

            if (Math.Abs(a.B - b.B) > (delta << 3))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 将 Rgb565 转换为 System.Drawing.Color
        /// </summary>
        public Color ToColor()
        {
            return Color.FromArgb(255, R, G, B);
        }

        /// <summary>
        /// 返回此颜色的字符串表示
        /// </summary>
        public override string ToString()
        {
            return $"RGB565: R={R}, G={G}, B={B} (0x{PackedValue:X4})";
        }
    }
}