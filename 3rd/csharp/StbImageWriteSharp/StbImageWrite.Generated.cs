namespace StbImageWriteSharp
{
	unsafe partial class StbImageWrite
	{
		public static int stbi__flip_vertically_on_write;

		public static int stbi_write_png_compression_level = 8;

		public static int stbi_write_force_png_filter = -1;

		public static ushort[] lengthc =
		{
			3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195,
			227, 258, 259
		};

		public static byte[] lengtheb =
			{0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0};

		public static ushort[] distc =
		{
			1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097,
			6145, 8193, 12289, 16385, 24577, 32768
		};

		public static byte[] disteb =
			{0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13};

		public static uint[] crc_table =
		{
			0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F, 0xE963A535, 0x9E6495A3, 0x0eDB8832,
			0x79DCB8A4, 0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91, 0x1DB71064, 0x6AB020F2,
			0xF3B97148, 0x84BE41DE, 0x1ADAD47D, 0x6DDDE4EB, 0xF4D4B551, 0x83D385C7, 0x136C9856, 0x646BA8C0, 0xFD62F97A,
			0x8A65C9EC, 0x14015C4F, 0x63066CD9, 0xFA0F3D63, 0x8D080DF5, 0x3B6E20C8, 0x4C69105E, 0xD56041E4, 0xA2677172,
			0x3C03E4D1, 0x4B04D447, 0xD20D85FD, 0xA50AB56B, 0x35B5A8FA, 0x42B2986C, 0xDBBBC9D6, 0xACBCF940, 0x32D86CE3,
			0x45DF5C75, 0xDCD60DCF, 0xABD13D59, 0x26D930AC, 0x51DE003A, 0xC8D75180, 0xBFD06116, 0x21B4F4B5, 0x56B3C423,
			0xCFBA9599, 0xB8BDA50F, 0x2802B89E, 0x5F058808, 0xC60CD9B2, 0xB10BE924, 0x2F6F7C87, 0x58684C11, 0xC1611DAB,
			0xB6662D3D, 0x76DC4190, 0x01DB7106, 0x98D220BC, 0xEFD5102A, 0x71B18589, 0x06B6B51F, 0x9FBFE4A5, 0xE8B8D433,
			0x7807C9A2, 0x0F00F934, 0x9609A88E, 0xE10E9818, 0x7F6A0DBB, 0x086D3D2D, 0x91646C97, 0xE6635C01, 0x6B6B51F4,
			0x1C6C6162, 0x856530D8, 0xF262004E, 0x6C0695ED, 0x1B01A57B, 0x8208F4C1, 0xF50FC457, 0x65B0D9C6, 0x12B7E950,
			0x8BBEB8EA, 0xFCB9887C, 0x62DD1DDF, 0x15DA2D49, 0x8CD37CF3, 0xFBD44C65, 0x4DB26158, 0x3AB551CE, 0xA3BC0074,
			0xD4BB30E2, 0x4ADFA541, 0x3DD895D7, 0xA4D1C46D, 0xD3D6F4FB, 0x4369E96A, 0x346ED9FC, 0xAD678846, 0xDA60B8D0,
			0x44042D73, 0x33031DE5, 0xAA0A4C5F, 0xDD0D7CC9, 0x5005713C, 0x270241AA, 0xBE0B1010, 0xC90C2086, 0x5768B525,
			0x206F85B3, 0xB966D409, 0xCE61E49F, 0x5EDEF90E, 0x29D9C998, 0xB0D09822, 0xC7D7A8B4, 0x59B33D17, 0x2EB40D81,
			0xB7BD5C3B, 0xC0BA6CAD, 0xEDB88320, 0x9ABFB3B6, 0x03B6E20C, 0x74B1D29A, 0xEAD54739, 0x9DD277AF, 0x04DB2615,
			0x73DC1683, 0xE3630B12, 0x94643B84, 0x0D6D6A3E, 0x7A6A5AA8, 0xE40ECF0B, 0x9309FF9D, 0x0A00AE27, 0x7D079EB1,
			0xF00F9344, 0x8708A3D2, 0x1E01F268, 0x6906C2FE, 0xF762575D, 0x806567CB, 0x196C3671, 0x6E6B06E7, 0xFED41B76,
			0x89D32BE0, 0x10DA7A5A, 0x67DD4ACC, 0xF9B9DF6F, 0x8EBEEFF9, 0x17B7BE43, 0x60B08ED5, 0xD6D6A3E8, 0xA1D1937E,
			0x38D8C2C4, 0x4FDFF252, 0xD1BB67F1, 0xA6BC5767, 0x3FB506DD, 0x48B2364B, 0xD80D2BDA, 0xAF0A1B4C, 0x36034AF6,
			0x41047A60, 0xDF60EFC3, 0xA867DF55, 0x316E8EEF, 0x4669BE79, 0xCB61B38C, 0xBC66831A, 0x256FD2A0, 0x5268E236,
			0xCC0C7795, 0xBB0B4703, 0x220216B9, 0x5505262F, 0xC5BA3BBE, 0xB2BD0B28, 0x2BB45A92, 0x5CB36A04, 0xC2D7FFA7,
			0xB5D0CF31, 0x2CD99E8B, 0x5BDEAE1D, 0x9B64C2B0, 0xEC63F226, 0x756AA39C, 0x026D930A, 0x9C0906A9, 0xEB0E363F,
			0x72076785, 0x05005713, 0x95BF4A82, 0xE2B87A14, 0x7BB12BAE, 0x0CB61B38, 0x92D28E9B, 0xE5D5BE0D, 0x7CDCEFB7,
			0x0BDBDF21, 0x86D3D2D4, 0xF1D4E242, 0x68DDB3F8, 0x1FDA836E, 0x81BE16CD, 0xF6B9265B, 0x6FB077E1, 0x18B74777,
			0x88085AE6, 0xFF0F6A70, 0x66063BCA, 0x11010B5C, 0x8F659EFF, 0xF862AE69, 0x616BFFD3, 0x166CCF45, 0xA00AE278,
			0xD70DD2EE, 0x4E048354, 0x3903B3C2, 0xA7672661, 0xD06016F7, 0x4969474D, 0x3E6E77DB, 0xAED16A4A, 0xD9D65ADC,
			0x40DF0B66, 0x37D83BF0, 0xA9BCAE53, 0xDEBB9EC5, 0x47B2CF7F, 0x30B5FFE9, 0xBDBDF21C, 0xCABAC28A, 0x53B39330,
			0x24B4A3A6, 0xBAD03605, 0xCDD70693, 0x54DE5729, 0x23D967BF, 0xB3667A2E, 0xC4614AB8, 0x5D681B02, 0x2A6F2B94,
			0xB40BBE37, 0xC30C8EA1, 0x5A05DF1B, 0x2D02EF8D
		};

		public static byte[] stbiw__jpg_ZigZag =
		{
			0, 1, 5, 6, 14, 15, 27, 28, 2, 4, 7, 13, 16, 26, 29, 42, 3, 8, 12, 17, 25, 30, 41, 43, 9, 11, 18, 24, 31,
			40, 44, 53, 10, 19, 23, 32, 39, 45, 52, 54, 20, 22, 33, 38, 46, 51, 55, 60, 21, 34, 37, 47, 50, 56, 59, 61,
			35, 36, 48, 49, 57, 58, 62, 63
		};

		public static byte[] std_dc_luminance_nrcodes = { 0, 0, 1, 5, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0 };

		public static byte[] std_dc_luminance_values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

		public static byte[] std_ac_luminance_nrcodes = { 0, 0, 2, 1, 3, 3, 2, 4, 3, 5, 5, 4, 4, 0, 0, 1, 0x7d };

		public static byte[] std_ac_luminance_values =
		{
			0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06, 0x13, 0x51, 0x61, 0x07, 0x22, 0x71,
			0x14, 0x32, 0x81, 0x91, 0xa1, 0x08, 0x23, 0x42, 0xb1, 0xc1, 0x15, 0x52, 0xd1, 0xf0, 0x24, 0x33, 0x62, 0x72,
			0x82, 0x09, 0x0a, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x34, 0x35, 0x36, 0x37,
			0x38, 0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
			0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x83,
			0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a, 0xa2, 0xa3,
			0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba, 0xc2, 0xc3,
			0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xe1, 0xe2,
			0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa
		};

		public static byte[] std_dc_chrominance_nrcodes = { 0, 0, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 };

		public static byte[] std_dc_chrominance_values = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

		public static byte[] std_ac_chrominance_nrcodes = { 0, 0, 2, 1, 2, 4, 4, 3, 4, 7, 5, 4, 4, 0, 1, 2, 0x77 };

		public static byte[] std_ac_chrominance_values =
		{
			0x00, 0x01, 0x02, 0x03, 0x11, 0x04, 0x05, 0x21, 0x31, 0x06, 0x12, 0x41, 0x51, 0x07, 0x61, 0x71, 0x13, 0x22,
			0x32, 0x81, 0x08, 0x14, 0x42, 0x91, 0xa1, 0xb1, 0xc1, 0x09, 0x23, 0x33, 0x52, 0xf0, 0x15, 0x62, 0x72, 0xd1,
			0x0a, 0x16, 0x24, 0x34, 0xe1, 0x25, 0xf1, 0x17, 0x18, 0x19, 0x1a, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x35, 0x36,
			0x37, 0x38, 0x39, 0x3a, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4a, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
			0x59, 0x5a, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a,
			0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8a, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9a,
			0xa2, 0xa3, 0xa4, 0xa5, 0xa6, 0xa7, 0xa8, 0xa9, 0xaa, 0xb2, 0xb3, 0xb4, 0xb5, 0xb6, 0xb7, 0xb8, 0xb9, 0xba,
			0xc2, 0xc3, 0xc4, 0xc5, 0xc6, 0xc7, 0xc8, 0xc9, 0xca, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda,
			0xe2, 0xe3, 0xe4, 0xe5, 0xe6, 0xe7, 0xe8, 0xe9, 0xea, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 0xfa
		};

		public static ushort[,] YDC_HT =
			{{0, 2}, {2, 3}, {3, 3}, {4, 3}, {5, 3}, {6, 3}, {14, 4}, {30, 5}, {62, 6}, {126, 7}, {254, 8}, {510, 9}};

		public static ushort[,] UVDC_HT =
		{
			{0, 2}, {1, 2}, {2, 2}, {6, 3}, {14, 4}, {30, 5}, {62, 6}, {126, 7}, {254, 8}, {510, 9}, {1022, 10},
			{2046, 11}
		};

		public static ushort[,] YAC_HT =
		{
			{10, 4}, {0, 2}, {1, 2}, {4, 3}, {11, 4}, {26, 5}, {120, 7}, {248, 8}, {1014, 10}, {65410, 16}, {65411, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {12, 4}, {27, 5}, {121, 7}, {502, 9}, {2038, 11},
			{65412, 16}, {65413, 16}, {65414, 16}, {65415, 16}, {65416, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {28, 5}, {249, 8}, {1015, 10}, {4084, 12}, {65417, 16}, {65418, 16}, {65419, 16}, {65420, 16},
			{65421, 16}, {65422, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {58, 6}, {503, 9}, {4085, 12},
			{65423, 16}, {65424, 16}, {65425, 16}, {65426, 16}, {65427, 16}, {65428, 16}, {65429, 16}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {59, 6}, {1016, 10}, {65430, 16}, {65431, 16}, {65432, 16}, {65433, 16},
			{65434, 16}, {65435, 16}, {65436, 16}, {65437, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{122, 7}, {2039, 11}, {65438, 16}, {65439, 16}, {65440, 16}, {65441, 16}, {65442, 16}, {65443, 16},
			{65444, 16}, {65445, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {123, 7}, {4086, 12}, {65446, 16},
			{65447, 16}, {65448, 16}, {65449, 16}, {65450, 16}, {65451, 16}, {65452, 16}, {65453, 16}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {250, 8}, {4087, 12}, {65454, 16}, {65455, 16}, {65456, 16}, {65457, 16},
			{65458, 16}, {65459, 16}, {65460, 16}, {65461, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{504, 9}, {32704, 15}, {65462, 16}, {65463, 16}, {65464, 16}, {65465, 16}, {65466, 16}, {65467, 16},
			{65468, 16}, {65469, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {505, 9}, {65470, 16},
			{65471, 16}, {65472, 16}, {65473, 16}, {65474, 16}, {65475, 16}, {65476, 16}, {65477, 16}, {65478, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {506, 9}, {65479, 16}, {65480, 16}, {65481, 16},
			{65482, 16}, {65483, 16}, {65484, 16}, {65485, 16}, {65486, 16}, {65487, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {1017, 10}, {65488, 16}, {65489, 16}, {65490, 16}, {65491, 16}, {65492, 16},
			{65493, 16}, {65494, 16}, {65495, 16}, {65496, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{1018, 10}, {65497, 16}, {65498, 16}, {65499, 16}, {65500, 16}, {65501, 16}, {65502, 16}, {65503, 16},
			{65504, 16}, {65505, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {2040, 11}, {65506, 16},
			{65507, 16}, {65508, 16}, {65509, 16}, {65510, 16}, {65511, 16}, {65512, 16}, {65513, 16}, {65514, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {65515, 16}, {65516, 16}, {65517, 16}, {65518, 16},
			{65519, 16}, {65520, 16}, {65521, 16}, {65522, 16}, {65523, 16}, {65524, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {2041, 11}, {65525, 16}, {65526, 16}, {65527, 16}, {65528, 16}, {65529, 16}, {65530, 16},
			{65531, 16}, {65532, 16}, {65533, 16}, {65534, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}
		};

		public static ushort[,] UVAC_HT =
		{
			{0, 2}, {1, 2}, {4, 3}, {10, 4}, {24, 5}, {25, 5}, {56, 6}, {120, 7}, {500, 9}, {1014, 10}, {4084, 12},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {11, 4}, {57, 6}, {246, 8}, {501, 9}, {2038, 11},
			{4085, 12}, {65416, 16}, {65417, 16}, {65418, 16}, {65419, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {26, 5}, {247, 8}, {1015, 10}, {4086, 12}, {32706, 15}, {65420, 16}, {65421, 16}, {65422, 16},
			{65423, 16}, {65424, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {27, 5}, {248, 8}, {1016, 10},
			{4087, 12}, {65425, 16}, {65426, 16}, {65427, 16}, {65428, 16}, {65429, 16}, {65430, 16}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {58, 6}, {502, 9}, {65431, 16}, {65432, 16}, {65433, 16}, {65434, 16},
			{65435, 16}, {65436, 16}, {65437, 16}, {65438, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {59, 6},
			{1017, 10}, {65439, 16}, {65440, 16}, {65441, 16}, {65442, 16}, {65443, 16}, {65444, 16}, {65445, 16},
			{65446, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {121, 7}, {2039, 11}, {65447, 16}, {65448, 16},
			{65449, 16}, {65450, 16}, {65451, 16}, {65452, 16}, {65453, 16}, {65454, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {122, 7}, {2040, 11}, {65455, 16}, {65456, 16}, {65457, 16}, {65458, 16},
			{65459, 16}, {65460, 16}, {65461, 16}, {65462, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{249, 8}, {65463, 16}, {65464, 16}, {65465, 16}, {65466, 16}, {65467, 16}, {65468, 16}, {65469, 16},
			{65470, 16}, {65471, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {503, 9}, {65472, 16},
			{65473, 16}, {65474, 16}, {65475, 16}, {65476, 16}, {65477, 16}, {65478, 16}, {65479, 16}, {65480, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {504, 9}, {65481, 16}, {65482, 16}, {65483, 16},
			{65484, 16}, {65485, 16}, {65486, 16}, {65487, 16}, {65488, 16}, {65489, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {0, 0}, {505, 9}, {65490, 16}, {65491, 16}, {65492, 16}, {65493, 16}, {65494, 16},
			{65495, 16}, {65496, 16}, {65497, 16}, {65498, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0},
			{506, 9}, {65499, 16}, {65500, 16}, {65501, 16}, {65502, 16}, {65503, 16}, {65504, 16}, {65505, 16},
			{65506, 16}, {65507, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {2041, 11}, {65508, 16},
			{65509, 16}, {65510, 16}, {65511, 16}, {65512, 16}, {65513, 16}, {65514, 16}, {65515, 16}, {65516, 16},
			{0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {16352, 14}, {65517, 16}, {65518, 16}, {65519, 16},
			{65520, 16}, {65521, 16}, {65522, 16}, {65523, 16}, {65524, 16}, {65525, 16}, {0, 0}, {0, 0}, {0, 0},
			{0, 0}, {0, 0}, {1018, 10}, {32707, 15}, {65526, 16}, {65527, 16}, {65528, 16}, {65529, 16}, {65530, 16},
			{65531, 16}, {65532, 16}, {65533, 16}, {65534, 16}, {0, 0}, {0, 0}, {0, 0}, {0, 0}, {0, 0}
		};

		public static int[] YQT =
		{
			16, 11, 10, 16, 24, 40, 51, 61, 12, 12, 14, 19, 26, 58, 60, 55, 14, 13, 16, 24, 40, 57, 69, 56, 14, 17, 22,
			29, 51, 87, 80, 62, 18, 22, 37, 56, 68, 109, 103, 77, 24, 35, 55, 64, 81, 104, 113, 92, 49, 64, 78, 87, 103,
			121, 120, 101, 72, 92, 95, 98, 112, 100, 103, 99
		};

		public static int[] UVQT =
		{
			17, 18, 24, 47, 99, 99, 99, 99, 18, 21, 26, 66, 99, 99, 99, 99, 24, 26, 56, 99, 99, 99, 99, 99, 47, 66, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99, 99,
			99, 99, 99, 99, 99, 99, 99, 99, 99, 99
		};

		public static float[] aasf =
		{
			1.0f * 2.828427125f, 1.387039845f * 2.828427125f, 1.306562965f * 2.828427125f, 1.175875602f * 2.828427125f,
			1.0f * 2.828427125f, 0.785694958f * 2.828427125f, 0.541196100f * 2.828427125f, 0.275899379f * 2.828427125f
		};

		public static byte[] head0 =
		{
			0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte) 'J', (byte) 'F', (byte) 'I', (byte) 'F', 0, 1, 1, 0, 0, 1, 0, 1, 0,
			0, 0xFF, 0xDB, 0, 0x84, 0
		};

		public static byte[] head2 = { 0xFF, 0xDA, 0, 0xC, 3, 1, 0, 2, 0x11, 3, 0x11, 0, 0x3F, 0 };

		public static void stbi_flip_vertically_on_write(int flag)
		{
			stbi__flip_vertically_on_write = flag;
		}

		public static void stbiw__putc(stbi__write_context s, byte c)
		{
			s.func(s.context, &c, 1);
		}

		public static void stbiw__write3(stbi__write_context s, byte a, byte b, byte c)
		{
			var arr = stackalloc byte[3];
			arr[0] = a;
			arr[1] = b;
			arr[2] = c;
			s.func(s.context, arr, 3);
		}

		public static void stbiw__write_pixel(stbi__write_context s, int rgb_dir, int comp, int write_alpha,
			int expand_mono, byte* d)
		{
			var bg = stackalloc byte[3];
			bg[0] = 255;
			bg[1] = 0;
			bg[2] = 255;
			var px = stackalloc byte[3];
			var k = 0;
			if (write_alpha < 0)
				s.func(s.context, &d[comp - 1], 1);
			switch (comp)
			{
				case 2:
				case 1:
					if (expand_mono != 0)
						stbiw__write3(s, d[0], d[0], d[0]);
					else
						s.func(s.context, d, 1);
					break;
				case 3:
				case 4:
					if (comp == 4 && write_alpha == 0)
					{
						for (k = 0; k < 3; ++k) px[k] = (byte)(bg[k] + (d[k] - bg[k]) * d[3] / 255);
						stbiw__write3(s, px[1 - rgb_dir], px[1], px[1 + rgb_dir]);
						break;
					}

					stbiw__write3(s, d[1 - rgb_dir], d[1], d[1 + rgb_dir]);
					break;
			}

			if (write_alpha > 0)
				s.func(s.context, &d[comp - 1], 1);
		}

		public static void stbiw__write_pixels(stbi__write_context s, int rgb_dir, int vdir, int x, int y, int comp,
			void* data, int write_alpha, int scanline_pad, int expand_mono)
		{
			var zero = (uint)0;
			var i = 0;
			var j = 0;
			var j_end = 0;
			if (y <= 0)
				return;
			if (stbi__flip_vertically_on_write != 0)
				vdir *= -1;
			if (vdir < 0)
			{
				j_end = -1;
				j = y - 1;
			}
			else
			{
				j_end = y;
				j = 0;
			}

			for (; j != j_end; j += vdir)
			{
				for (i = 0; i < x; ++i)
				{
					var d = (byte*)data + (j * x + i) * comp;
					stbiw__write_pixel(s, rgb_dir, comp, write_alpha, expand_mono, d);
				}

				s.func(s.context, &zero, scanline_pad);
			}
		}

		public static int stbi_write_bmp_core(stbi__write_context s, int x, int y, int comp, void* data)
		{
			var pad = (-x * 3) & 3;
			return stbiw__outfile(s, -1, -1, x, y, comp, 1, data, 0, pad, "11 4 22 44 44 22 444444", (int)'B',
				(int)'M', 14 + 40 + (x * 3 + pad) * y, 0, 0, 14 + 40, 40, x, y, 1, 24, 0, 0, 0, 0, 0, 0);
		}

		public static int stbi_write_tga_core(stbi__write_context s, int x, int y, int comp, void* data)
		{
			var has_alpha = comp == 2 || comp == 4 ? 1 : 0;
			var colorbytes = has_alpha != 0 ? comp - 1 : comp;
			var format = colorbytes < 2 ? 3 : 2;
			if (y < 0 || x < 0)
				return 0;
			if (stbi_write_tga_with_rle == 0)
				return stbiw__outfile(s, -1, -1, x, y, comp, 0, data, has_alpha, 0, "111 221 2222 11", 0, 0, format, 0,
					0, 0, 0, 0, x, y, (colorbytes + has_alpha) * 8, has_alpha * 8);

			var i = 0;
			var j = 0;
			var k = 0;
			var jend = 0;
			var jdir = 0;
			stbiw__writef(s, "111 221 2222 11", 0, 0, format + 8, 0, 0, 0, 0, 0, x, y, (colorbytes + has_alpha) * 8,
				has_alpha * 8);
			if (stbi__flip_vertically_on_write != 0)
			{
				j = 0;
				jend = y;
				jdir = 1;
			}
			else
			{
				j = y - 1;
				jend = -1;
				jdir = -1;
			}

			for (; j != jend; j += jdir)
			{
				var row = (byte*)data + j * x * comp;
				var len = 0;
				for (i = 0; i < x; i += len)
				{
					var begin = row + i * comp;
					var diff = 1;
					len = 1;
					if (i < x - 1)
					{
						++len;
						diff = CRuntime.memcmp(begin, row + (i + 1) * comp, (ulong)comp);
						if (diff != 0)
						{
							var prev = begin;
							for (k = i + 2; k < x && len < 128; ++k)
								if (CRuntime.memcmp(prev, row + k * comp, (ulong)comp) != 0)
								{
									prev += comp;
									++len;
								}
								else
								{
									--len;
									break;
								}
						}
						else
						{
							for (k = i + 2; k < x && len < 128; ++k)
								if (CRuntime.memcmp(begin, row + k * comp, (ulong)comp) == 0)
									++len;
								else
									break;
						}
					}

					if (diff != 0)
					{
						var header = (byte)((len - 1) & 0xff);
						s.func(s.context, &header, 1);
						for (k = 0; k < len; ++k) stbiw__write_pixel(s, -1, comp, has_alpha, 0, begin + k * comp);
					}
					else
					{
						var header = (byte)((len - 129) & 0xff);
						s.func(s.context, &header, 1);
						stbiw__write_pixel(s, -1, comp, has_alpha, 0, begin);
					}
				}
			}

			return 1;
		}

		public static void stbiw__linear_to_rgbe(byte* rgbe, float* linear)
		{
			var exponent = 0;
			var maxcomp = linear[0] > (linear[1] > linear[2] ? linear[1] : linear[2]) ? linear[0] :
				linear[1] > linear[2] ? linear[1] : linear[2];
			if (maxcomp < 1e-32f)
			{
				rgbe[0] = rgbe[1] = rgbe[2] = rgbe[3] = 0;
			}
			else
			{
				var normalize = (float)CRuntime.frexp(maxcomp, &exponent) * 256.0f / maxcomp;
				rgbe[0] = (byte)(linear[0] * normalize);
				rgbe[1] = (byte)(linear[1] * normalize);
				rgbe[2] = (byte)(linear[2] * normalize);
				rgbe[3] = (byte)(exponent + 128);
			}
		}

		public static void stbiw__write_run_data(stbi__write_context s, int length, byte databyte)
		{
			var lengthbyte = (byte)((length + 128) & 0xff);
			s.func(s.context, &lengthbyte, 1);
			s.func(s.context, &databyte, 1);
		}

		public static void stbiw__write_dump_data(stbi__write_context s, int length, byte* data)
		{
			var lengthbyte = (byte)(length & 0xff);
			s.func(s.context, &lengthbyte, 1);
			s.func(s.context, data, length);
		}

		public static void stbiw__write_hdr_scanline(stbi__write_context s, int width, int ncomp, byte* scratch,
			float* scanline)
		{
			var scanlineheader = stackalloc byte[4];
			scanlineheader[0] = 2;
			scanlineheader[1] = 2;
			scanlineheader[2] = 0;
			scanlineheader[3] = 0;

			var rgbe = stackalloc byte[4];
			var linear = stackalloc float[3];
			var x = 0;
			scanlineheader[2] = (byte)((width & 0xff00) >> 8);
			scanlineheader[3] = (byte)(width & 0x00ff);
			if (width < 8 || width >= 32768)
			{
				for (x = 0; x < width; x++)
				{
					switch (ncomp)
					{
						case 4:
						case 3:
							linear[2] = scanline[x * ncomp + 2];
							linear[1] = scanline[x * ncomp + 1];
							linear[0] = scanline[x * ncomp + 0];
							break;
						default:
							linear[0] = linear[1] = linear[2] = scanline[x * ncomp + 0];
							break;
					}

					stbiw__linear_to_rgbe(rgbe, linear);
					s.func(s.context, rgbe, 4);
				}
			}
			else
			{
				var c = 0;
				var r = 0;
				for (x = 0; x < width; x++)
				{
					switch (ncomp)
					{
						case 4:
						case 3:
							linear[2] = scanline[x * ncomp + 2];
							linear[1] = scanline[x * ncomp + 1];
							linear[0] = scanline[x * ncomp + 0];
							break;
						default:
							linear[0] = linear[1] = linear[2] = scanline[x * ncomp + 0];
							break;
					}

					stbiw__linear_to_rgbe(rgbe, linear);
					scratch[x + width * 0] = rgbe[0];
					scratch[x + width * 1] = rgbe[1];
					scratch[x + width * 2] = rgbe[2];
					scratch[x + width * 3] = rgbe[3];
				}

				s.func(s.context, scanlineheader, 4);
				for (c = 0; c < 4; c++)
				{
					var comp = &scratch[width * c];
					x = 0;
					while (x < width)
					{
						r = x;
						while (r + 2 < width)
						{
							if (comp[r] == comp[r + 1] && comp[r] == comp[r + 2])
								break;
							++r;
						}

						if (r + 2 >= width)
							r = width;
						while (x < r)
						{
							var len = r - x;
							if (len > 128)
								len = 128;
							stbiw__write_dump_data(s, len, &comp[x]);
							x += len;
						}

						if (r + 2 < width)
						{
							while (r < width && comp[r] == comp[x]) ++r;
							while (x < r)
							{
								var len = r - x;
								if (len > 127)
									len = 127;
								stbiw__write_run_data(s, len, comp[x]);
								x += len;
							}
						}
					}
				}
			}
		}

		public static void* stbiw__sbgrowf(void** arr, int increment, int itemsize)
		{
			var m = *arr != null ? 2 * ((int*)*arr - 2)[0] + increment : increment + 1;
			var p = CRuntime.realloc(*arr != null ? (int*)*arr - 2 : null, (ulong)(itemsize * m + sizeof(int) * 2));
			if (p != null)
			{
				if (*arr == null)
					((int*)p)[1] = 0;
				*arr = (int*)p + 2;
				((int*)*arr - 2)[0] = m;
			}

			return *arr;
		}

		public static byte* stbiw__zlib_flushf(byte* data, uint* bitbuffer, int* bitcount)
		{
			while (*bitcount >= 8)
			{
				if (data == null || ((int*)data - 2)[1] + 1 >= ((int*)data - 2)[0])
					stbiw__sbgrowf((void**)&data, 1, sizeof(byte));
				data[((int*)data - 2)[1]++] = (byte)(*bitbuffer & 0xff);
				*bitbuffer >>= 8;
				*bitcount -= 8;
			}

			return data;
		}

		public static int stbiw__zlib_bitrev(int code, int codebits)
		{
			var res = 0;
			while (codebits-- != 0)
			{
				res = (res << 1) | (code & 1);
				code >>= 1;
			}

			return res;
		}

		public static uint stbiw__zlib_countm(byte* a, byte* b, int limit)
		{
			var i = 0;
			for (i = 0; i < limit && i < 258; ++i)
				if (a[i] != b[i])
					break;
			return (uint)i;
		}

		public static uint stbiw__zhash(byte* data)
		{
			var hash = (uint)(data[0] + (data[1] << 8) + (data[2] << 16));
			hash ^= hash << 3;
			hash += hash >> 5;
			hash ^= hash << 4;
			hash += hash >> 17;
			hash ^= hash << 25;
			hash += hash >> 6;
			return hash;
		}

		public static byte* stbi_zlib_compress(byte* data, int data_len, int* out_len, int quality)
		{
			var bitbuf = (uint)0;
			var i = 0;
			var j = 0;
			var bitcount = 0;
			byte* _out_ = null;
			var hash_table = (byte***)CRuntime.malloc((ulong)(16384 * sizeof(char**)));
			if (hash_table == null)
				return null;
			if (quality < 5)
				quality = 5;
			if (_out_ == null || ((int*)_out_ - 2)[1] + 1 >= ((int*)_out_ - 2)[0])
				stbiw__sbgrowf((void**)&_out_, 1, sizeof(byte));

			_out_[((int*)_out_ - 2)[1]++] = 0x78;
			if (_out_ == null || ((int*)_out_ - 2)[1] + 1 >= ((int*)_out_ - 2)[0])
				stbiw__sbgrowf((void**)&_out_, 1, sizeof(byte));

			_out_[((int*)_out_ - 2)[1]++] = 0x5e;
			{
				bitbuf |= (uint)(1 << bitcount);
				bitcount += 1;
				_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
			}

			{
				bitbuf |= (uint)(1 << bitcount);
				bitcount += 2;
				_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
			}

			for (i = 0; i < 16384; ++i) hash_table[i] = null;
			i = 0;
			while (i < data_len - 3)
			{
				var h = (int)(stbiw__zhash(data + i) & (16384 - 1));
				var best = 3;
				byte* bestloc = null;
				var hlist = hash_table[h];
				var n = hlist != null ? ((int*)hlist - 2)[1] : 0;
				for (j = 0; j < n; ++j)
					if (hlist[j] - data > i - 32768)
					{
						var d = (int)stbiw__zlib_countm(hlist[j], data + i, data_len - i);
						if (d >= best)
						{
							best = d;
							bestloc = hlist[j];
						}
					}

				if (hash_table[h] != null && ((int*)hash_table[h] - 2)[1] == 2 * quality)
				{
					CRuntime.memmove(hash_table[h], hash_table[h] + quality, (ulong)(sizeof(byte*) * quality));
					((int*)hash_table[h] - 2)[1] = quality;
				}

				if (hash_table[h] == null || ((int*)hash_table[h] - 2)[1] + 1 >= ((int*)hash_table[h] - 2)[0])
					stbiw__sbgrowf((void**)&hash_table[h], 1, sizeof(byte*));
				hash_table[h][((int*)hash_table[h] - 2)[1]++] = data + i;
				if (bestloc != null)
				{
					h = (int)(stbiw__zhash(data + i + 1) & (16384 - 1));
					hlist = hash_table[h];
					n = hlist != null ? ((int*)hlist - 2)[1] : 0;
					for (j = 0; j < n; ++j)
						if (hlist[j] - data > i - 32767)
						{
							var e = (int)stbiw__zlib_countm(hlist[j], data + i + 1, data_len - i - 1);
							if (e > best)
							{
								bestloc = null;
								break;
							}
						}
				}

				if (bestloc != null)
				{
					var d = (int)(data + i - bestloc);
					for (j = 0; best > lengthc[j + 1] - 1; ++j)
					{
					}

					if (j + 257 <= 143)
					{
						bitbuf |= (uint)(stbiw__zlib_bitrev(0x30 + j + 257, 8) << bitcount);
						bitcount += 8;
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}
					else if (j + 257 <= 255)
					{
						bitbuf |= (uint)(stbiw__zlib_bitrev(0x190 + j + 257 - 144, 9) << bitcount);
						bitcount += 9;
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}
					else if (j + 257 <= 279)
					{
						bitbuf |= (uint)(stbiw__zlib_bitrev(0 + j + 257 - 256, 7) << bitcount);
						bitcount += 7;
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}
					else
					{
						bitbuf |= (uint)(stbiw__zlib_bitrev(0xc0 + j + 257 - 280, 8) << bitcount);
						bitcount += 8;
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}

					if (lengtheb[j] != 0)
					{
						bitbuf |= (uint)((best - lengthc[j]) << bitcount);
						bitcount += lengtheb[j];
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}

					for (j = 0; d > distc[j + 1] - 1; ++j)
					{
					}

					{
						bitbuf |= (uint)(stbiw__zlib_bitrev(j, 5) << bitcount);
						bitcount += 5;
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}
					if (disteb[j] != 0)
					{
						bitbuf |= (uint)((d - distc[j]) << bitcount);
						bitcount += disteb[j];
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}

					i += best;
				}
				else
				{
					if (data[i] <= 143)
					{
						bitbuf |= (uint)(stbiw__zlib_bitrev(0x30 + data[i], 8) << bitcount);
						bitcount += 8;
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}
					else
					{
						bitbuf |= (uint)(stbiw__zlib_bitrev(0x190 + data[i] - 144, 9) << bitcount);
						bitcount += 9;
						_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
					}

					++i;
				}
			}

			for (; i < data_len; ++i)
				if (data[i] <= 143)
				{
					bitbuf |= (uint)(stbiw__zlib_bitrev(0x30 + data[i], 8) << bitcount);
					bitcount += 8;
					_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
				}
				else
				{
					bitbuf |= (uint)(stbiw__zlib_bitrev(0x190 + data[i] - 144, 9) << bitcount);
					bitcount += 9;
					_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
				}

			bitbuf |= (uint)(stbiw__zlib_bitrev(0, 7) << bitcount);
			bitcount += 7;
			_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);

			while (bitcount != 0)
			{
				bitbuf |= (uint)(0 << bitcount);
				bitcount += 1;
				_out_ = stbiw__zlib_flushf(_out_, &bitbuf, &bitcount);
			}

			for (i = 0; i < 16384; ++i)
				if (hash_table[i] != null)
					CRuntime.free((int*)hash_table[i] - 2);
			CRuntime.free(hash_table);
			{
				var s1 = (uint)1;
				var s2 = (uint)0;
				var blocklen = data_len % 5552;
				j = 0;
				while (j < data_len)
				{
					for (i = 0; i < blocklen; ++i)
					{
						s1 += data[j + i];
						s2 += s1;
					}

					s1 %= 65521;
					s2 %= 65521;
					j += blocklen;
					blocklen = 5552;
				}

				if (_out_ == null || ((int*)_out_ - 2)[1] + 1 >= ((int*)_out_ - 2)[0])
					stbiw__sbgrowf((void**)&_out_, 1, sizeof(byte));
				_out_[((int*)_out_ - 2)[1]++] = (byte)((s2 >> 8) & 0xff);
				if (_out_ == null || ((int*)_out_ - 2)[1] + 1 >= ((int*)_out_ - 2)[0])
					stbiw__sbgrowf((void**)&_out_, 1, sizeof(byte));
				_out_[((int*)_out_ - 2)[1]++] = (byte)(s2 & 0xff);
				if (_out_ == null || ((int*)_out_ - 2)[1] + 1 >= ((int*)_out_ - 2)[0])
					stbiw__sbgrowf((void**)&_out_, 1, sizeof(byte));
				_out_[((int*)_out_ - 2)[1]++] = (byte)((s1 >> 8) & 0xff);
				if (_out_ == null || ((int*)_out_ - 2)[1] + 1 >= ((int*)_out_ - 2)[0])
					stbiw__sbgrowf((void**)&_out_, 1, sizeof(byte));
				_out_[((int*)_out_ - 2)[1]++] = (byte)(s1 & 0xff);
			}

			*out_len = ((int*)_out_ - 2)[1];
			CRuntime.memmove((int*)_out_ - 2, _out_, (ulong)*out_len);
			return (byte*)((int*)_out_ - 2);
		}

		public static uint stbiw__crc32(byte* buffer, int len)
		{
			var crc = ~0u;
			var i = 0;
			for (i = 0; i < len; ++i) crc = (crc >> 8) ^ crc_table[buffer[i] ^ (crc & 0xff)];
			return ~crc;
		}

		public static void stbiw__wpcrc(byte** data, int len)
		{
			var crc = stbiw__crc32(*data - len - 4, len + 4);
			(*data)[0] = (byte)((crc >> 24) & 0xff);
			(*data)[1] = (byte)((crc >> 16) & 0xff);
			(*data)[2] = (byte)((crc >> 8) & 0xff);
			(*data)[3] = (byte)(crc & 0xff);
			(*data) += 4;
		}

		public static byte stbiw__paeth(int a, int b, int c)
		{
			var p = a + b - c;
			var pa = CRuntime.abs(p - a);
			var pb = CRuntime.abs(p - b);
			var pc = CRuntime.abs(p - c);
			if (pa <= pb && pa <= pc)
				return (byte)(a & 0xff);
			if (pb <= pc)
				return (byte)(b & 0xff);
			return (byte)(c & 0xff);
		}

		public static void stbiw__encode_png_line(byte* pixels, int stride_bytes, int width, int height, int y, int n,
			int filter_type, sbyte* line_buffer)
		{
			var mapping = stackalloc int[5];
			mapping[0] = 0;
			mapping[1] = 1;
			mapping[2] = 2;
			mapping[3] = 3;
			mapping[4] = 4;

			var firstmap = stackalloc int[5];
			firstmap[0] = 0;
			firstmap[1] = 1;
			firstmap[2] = 0;
			firstmap[3] = 5;
			firstmap[4] = 6;

			var mymap = y != 0 ? mapping : firstmap;
			var i = 0;
			var type = mymap[filter_type];
			var z = pixels + stride_bytes * (stbi__flip_vertically_on_write != 0 ? height - 1 - y : y);
			var signed_stride = stbi__flip_vertically_on_write != 0 ? -stride_bytes : stride_bytes;
			if (type == 0)
			{
				CRuntime.memcpy(line_buffer, z, (ulong)(width * n));
				return;
			}

			for (i = 0; i < n; ++i)
				switch (type)
				{
					case 1:
						line_buffer[i] = (sbyte)z[i];
						break;
					case 2:
						line_buffer[i] = (sbyte)(z[i] - z[i - signed_stride]);
						break;
					case 3:
						line_buffer[i] = (sbyte)(z[i] - (z[i - signed_stride] >> 1));
						break;
					case 4:
						line_buffer[i] = (sbyte)(z[i] - stbiw__paeth(0, z[i - signed_stride], 0));
						break;
					case 5:
						line_buffer[i] = (sbyte)z[i];
						break;
					case 6:
						line_buffer[i] = (sbyte)z[i];
						break;
				}

			switch (type)
			{
				case 1:
					for (i = n; i < width * n; ++i) line_buffer[i] = (sbyte)(z[i] - z[i - n]);
					break;
				case 2:
					for (i = n; i < width * n; ++i) line_buffer[i] = (sbyte)(z[i] - z[i - signed_stride]);
					break;
				case 3:
					for (i = n; i < width * n; ++i)
						line_buffer[i] = (sbyte)(z[i] - ((z[i - n] + z[i - signed_stride]) >> 1));
					break;
				case 4:
					for (i = n; i < width * n; ++i)
						line_buffer[i] =
							(sbyte)(z[i] - stbiw__paeth(z[i - n], z[i - signed_stride], z[i - signed_stride - n]));
					break;
				case 5:
					for (i = n; i < width * n; ++i) line_buffer[i] = (sbyte)(z[i] - (z[i - n] >> 1));
					break;
				case 6:
					for (i = n; i < width * n; ++i) line_buffer[i] = (sbyte)(z[i] - stbiw__paeth(z[i - n], 0, 0));
					break;
			}
		}

		public static byte* stbi_write_png_to_mem(byte* pixels, int stride_bytes, int x, int y, int n, int* out_len)
		{
			var force_filter = stbi_write_force_png_filter;
			var ctype = stackalloc int[5];
			ctype[0] = -1;
			ctype[1] = 0;
			ctype[2] = 4;
			ctype[3] = 2;
			ctype[4] = 6;

			var sig = stackalloc byte[8];
			sig[0] = 137;
			sig[1] = 80;
			sig[2] = 78;
			sig[3] = 71;
			sig[4] = 13;
			sig[5] = 10;
			sig[6] = 26;
			sig[7] = 10;

			byte* _out_;
			byte* o;
			byte* filt;
			byte* zlib;
			sbyte* line_buffer;
			var j = 0;
			var zlen = 0;
			if (stride_bytes == 0)
				stride_bytes = x * n;
			if (force_filter >= 5) force_filter = -1;

			filt = (byte*)CRuntime.malloc((ulong)((x * n + 1) * y));
			if (filt == null)
				return null;
			line_buffer = (sbyte*)CRuntime.malloc((ulong)(x * n));
			if (line_buffer == null)
			{
				CRuntime.free(filt);
				return null;
			}

			for (j = 0; j < y; ++j)
			{
				var filter_type = 0;
				if (force_filter > -1)
				{
					filter_type = force_filter;
					stbiw__encode_png_line(pixels, stride_bytes, x, y, j, n, force_filter, line_buffer);
				}
				else
				{
					var best_filter = 0;
					var best_filter_val = 0x7fffffff;
					var est = 0;
					var i = 0;
					for (filter_type = 0; filter_type < 5; filter_type++)
					{
						stbiw__encode_png_line(pixels, stride_bytes, x, y, j, n, filter_type, line_buffer);
						est = 0;
						for (i = 0; i < x * n; ++i) est += CRuntime.abs(line_buffer[i]);
						if (est < best_filter_val)
						{
							best_filter_val = est;
							best_filter = filter_type;
						}
					}

					if (filter_type != best_filter)
					{
						stbiw__encode_png_line(pixels, stride_bytes, x, y, j, n, best_filter, line_buffer);
						filter_type = best_filter;
					}
				}

				filt[j * (x * n + 1)] = (byte)filter_type;
				CRuntime.memmove(filt + j * (x * n + 1) + 1, line_buffer, (ulong)(x * n));
			}

			CRuntime.free(line_buffer);
			zlib = stbi_zlib_compress(filt, y * (x * n + 1), &zlen, stbi_write_png_compression_level);
			CRuntime.free(filt);
			if (zlib == null)
				return null;
			_out_ = (byte*)CRuntime.malloc((ulong)(8 + 12 + 13 + 12 + zlen + 12));
			if (_out_ == null)
				return null;
			*out_len = 8 + 12 + 13 + 12 + zlen + 12;
			o = _out_;
			CRuntime.memmove(o, sig, (ulong)8);
			o += 8;
			o[0] = (13 >> 24) & 0xff;
			o[1] = (13 >> 16) & 0xff;
			o[2] = (13 >> 8) & 0xff;
			o[3] = 13 & 0xff;
			o += 4;
			o[0] = (byte)("IHDR"[0] & 0xff);
			o[1] = (byte)("IHDR"[1] & 0xff);
			o[2] = (byte)("IHDR"[2] & 0xff);
			o[3] = (byte)("IHDR"[3] & 0xff);
			o += 4;
			o[0] = (byte)((x >> 24) & 0xff);
			o[1] = (byte)((x >> 16) & 0xff);
			o[2] = (byte)((x >> 8) & 0xff);
			o[3] = (byte)(x & 0xff);
			o += 4;
			o[0] = (byte)((y >> 24) & 0xff);
			o[1] = (byte)((y >> 16) & 0xff);
			o[2] = (byte)((y >> 8) & 0xff);
			o[3] = (byte)(y & 0xff);
			o += 4;
			*o++ = 8;
			*o++ = (byte)(ctype[n] & 0xff);
			*o++ = 0;
			*o++ = 0;
			*o++ = 0;
			stbiw__wpcrc(&o, 13);
			o[0] = (byte)((zlen >> 24) & 0xff);
			o[1] = (byte)((zlen >> 16) & 0xff);
			o[2] = (byte)((zlen >> 8) & 0xff);
			o[3] = (byte)(zlen & 0xff);
			o += 4;
			o[0] = (byte)("IDAT"[0] & 0xff);
			o[1] = (byte)("IDAT"[1] & 0xff);
			o[2] = (byte)("IDAT"[2] & 0xff);
			o[3] = (byte)("IDAT"[3] & 0xff);
			o += 4;
			CRuntime.memmove(o, zlib, (ulong)zlen);
			o += zlen;
			CRuntime.free(zlib);
			stbiw__wpcrc(&o, zlen);
			o[0] = (0 >> 24) & 0xff;
			o[1] = (0 >> 16) & 0xff;
			o[2] = (0 >> 8) & 0xff;
			o[3] = 0 & 0xff;
			o += 4;
			o[0] = (byte)("IEND"[0] & 0xff);
			o[1] = (byte)("IEND"[1] & 0xff);
			o[2] = (byte)("IEND"[2] & 0xff);
			o[3] = (byte)("IEND"[3] & 0xff);
			o += 4;
			stbiw__wpcrc(&o, 0);
			return _out_;
		}

		public static void stbiw__jpg_writeBits(stbi__write_context s, int* bitBufP, int* bitCntP, ushort bs0,
			ushort bs1)
		{
			var bitBuf = *bitBufP;
			var bitCnt = *bitCntP;
			bitCnt += bs1;
			bitBuf |= bs0 << (24 - bitCnt);
			while (bitCnt >= 8)
			{
				var c = (byte)((bitBuf >> 16) & 255);
				stbiw__putc(s, c);
				if (c == 255) stbiw__putc(s, 0);
				bitBuf <<= 8;
				bitCnt -= 8;
			}

			*bitBufP = bitBuf;
			*bitCntP = bitCnt;
		}

		public static void stbiw__jpg_DCT(float* d0p, float* d1p, float* d2p, float* d3p, float* d4p, float* d5p,
			float* d6p, float* d7p)
		{
			var d0 = *d0p;
			var d1 = *d1p;
			var d2 = *d2p;
			var d3 = *d3p;
			var d4 = *d4p;
			var d5 = *d5p;
			var d6 = *d6p;
			var d7 = *d7p;
			float z1 = 0;
			float z2 = 0;
			float z3 = 0;
			float z4 = 0;
			float z5 = 0;
			float z11 = 0;
			float z13 = 0;
			var tmp0 = d0 + d7;
			var tmp7 = d0 - d7;
			var tmp1 = d1 + d6;
			var tmp6 = d1 - d6;
			var tmp2 = d2 + d5;
			var tmp5 = d2 - d5;
			var tmp3 = d3 + d4;
			var tmp4 = d3 - d4;
			var tmp10 = tmp0 + tmp3;
			var tmp13 = tmp0 - tmp3;
			var tmp11 = tmp1 + tmp2;
			var tmp12 = tmp1 - tmp2;
			d0 = tmp10 + tmp11;
			d4 = tmp10 - tmp11;
			z1 = (tmp12 + tmp13) * 0.707106781f;
			d2 = tmp13 + z1;
			d6 = tmp13 - z1;
			tmp10 = tmp4 + tmp5;
			tmp11 = tmp5 + tmp6;
			tmp12 = tmp6 + tmp7;
			z5 = (tmp10 - tmp12) * 0.382683433f;
			z2 = tmp10 * 0.541196100f + z5;
			z4 = tmp12 * 1.306562965f + z5;
			z3 = tmp11 * 0.707106781f;
			z11 = tmp7 + z3;
			z13 = tmp7 - z3;
			*d5p = z13 + z2;
			*d3p = z13 - z2;
			*d1p = z11 + z4;
			*d7p = z11 - z4;
			*d0p = d0;
			*d2p = d2;
			*d4p = d4;
			*d6p = d6;
		}

		public static void stbiw__jpg_calcBits(int val, ushort* bits)
		{
			var tmp1 = val < 0 ? -val : val;
			val = val < 0 ? val - 1 : val;
			bits[1] = 1;
			while ((tmp1 >>= 1) != 0) ++bits[1];
			bits[0] = (ushort)(val & ((1 << bits[1]) - 1));
		}

		public static int stbiw__jpg_processDU(stbi__write_context s, int* bitBuf, int* bitCnt, float* CDU,
			float* fdtbl, int DC, ushort[,] HTDC, ushort[,] HTAC)
		{
			var EOB = stackalloc ushort[2];
			EOB[0] = HTAC[0x00, 0];
			EOB[1] = HTAC[0x00, 1];

			var M16zeroes = stackalloc ushort[2];
			M16zeroes[0] = HTAC[0xF0, 0];
			M16zeroes[1] = HTAC[0xF0, 1];

			var dataOff = 0;
			var i = 0;
			var diff = 0;
			var end0pos = 0;
			var DU = stackalloc int[64];
			for (dataOff = 0; dataOff < 64; dataOff += 8)
				stbiw__jpg_DCT(&CDU[dataOff], &CDU[dataOff + 1], &CDU[dataOff + 2], &CDU[dataOff + 3],
					&CDU[dataOff + 4], &CDU[dataOff + 5], &CDU[dataOff + 6], &CDU[dataOff + 7]);
			for (dataOff = 0; dataOff < 8; ++dataOff)
				stbiw__jpg_DCT(&CDU[dataOff], &CDU[dataOff + 8], &CDU[dataOff + 16], &CDU[dataOff + 24],
					&CDU[dataOff + 32], &CDU[dataOff + 40], &CDU[dataOff + 48], &CDU[dataOff + 56]);
			for (i = 0; i < 64; ++i)
			{
				var v = CDU[i] * fdtbl[i];
				DU[stbiw__jpg_ZigZag[i]] = (int)(v < 0 ? v - 0.5f : v + 0.5f);
			}

			diff = DU[0] - DC;
			if (diff == 0)
			{
				stbiw__jpg_writeBits(s, bitBuf, bitCnt, HTDC[0, 0], HTDC[0, 1]);
			}
			else
			{
				var bits = stackalloc ushort[2];
				stbiw__jpg_calcBits(diff, bits);
				stbiw__jpg_writeBits(s, bitBuf, bitCnt, HTDC[bits[1], 0], HTDC[bits[1], 1]);
				stbiw__jpg_writeBits(s, bitBuf, bitCnt, bits[0], bits[1]);
			}

			end0pos = 63;
			for (; end0pos > 0 && DU[end0pos] == 0; --end0pos)
			{
			}

			if (end0pos == 0)
			{
				stbiw__jpg_writeBits(s, bitBuf, bitCnt, EOB[0], EOB[1]);
				return DU[0];
			}

			for (i = 1; i <= end0pos; ++i)
			{
				var startpos = i;
				var nrzeroes = 0;
				var bits = stackalloc ushort[2];
				for (; DU[i] == 0 && i <= end0pos; ++i)
				{
				}

				nrzeroes = i - startpos;
				if (nrzeroes >= 16)
				{
					var lng = nrzeroes >> 4;
					var nrmarker = 0;
					for (nrmarker = 1; nrmarker <= lng; ++nrmarker)
						stbiw__jpg_writeBits(s, bitBuf, bitCnt, M16zeroes[0], M16zeroes[1]);
					nrzeroes &= 15;
				}

				stbiw__jpg_calcBits(DU[i], bits);
				stbiw__jpg_writeBits(s, bitBuf, bitCnt, HTAC[(nrzeroes << 4) + bits[1], 0],
					HTAC[(nrzeroes << 4) + bits[1], 1]);
				stbiw__jpg_writeBits(s, bitBuf, bitCnt, bits[0], bits[1]);
			}

			if (end0pos != 63) stbiw__jpg_writeBits(s, bitBuf, bitCnt, EOB[0], EOB[1]);

			return DU[0];
		}

		public static int stbi_write_jpg_core(stbi__write_context s, int width, int height, int comp, void* data,
			int quality)
		{
			var row = 0;
			var col = 0;
			var i = 0;
			var k = 0;
			var fdtbl_Y = stackalloc float[64];
			var fdtbl_UV = stackalloc float[64];
			var YTable = stackalloc byte[64];
			var UVTable = stackalloc byte[64];
			if (data == null || width == 0 || height == 0 || comp > 4 || comp < 1) return 0;

			quality = quality != 0 ? quality : 90;
			quality = quality < 1 ? 1 : quality > 100 ? 100 : quality;
			quality = quality < 50 ? 5000 / quality : 200 - quality * 2;
			for (i = 0; i < 64; ++i)
			{
				var uvti = 0;
				var yti = (YQT[i] * quality + 50) / 100;
				YTable[stbiw__jpg_ZigZag[i]] = (byte)(yti < 1 ? 1 : yti > 255 ? 255 : yti);
				uvti = (UVQT[i] * quality + 50) / 100;
				UVTable[stbiw__jpg_ZigZag[i]] = (byte)(uvti < 1 ? 1 : uvti > 255 ? 255 : uvti);
			}

			for (row = 0, k = 0; row < 8; ++row)
				for (col = 0; col < 8; ++col, ++k)
				{
					fdtbl_Y[k] = 1 / (YTable[stbiw__jpg_ZigZag[k]] * aasf[row] * aasf[col]);
					fdtbl_UV[k] = 1 / (UVTable[stbiw__jpg_ZigZag[k]] * aasf[row] * aasf[col]);
				}

			{
				var head1 = stackalloc byte[24];
				head1[0] = 0xFF;
				head1[1] = 0xC0;
				head1[2] = 0;
				head1[3] = 0x11;
				head1[4] = 8;
				head1[5] = (byte)(height >> 8);
				head1[6] = (byte)(height & 0xff);
				head1[7] = (byte)(width >> 8);
				head1[8] = (byte)(width & 0xff);
				head1[9] = 3;
				head1[10] = 1;
				head1[11] = 0x11;
				head1[12] = 0;
				head1[13] = 2;
				head1[14] = 0x11;
				head1[15] = 1;
				head1[16] = 3;
				head1[17] = 0x11;
				head1[18] = 1;
				head1[19] = 0xFF;
				head1[20] = 0xC4;
				head1[21] = 0x01;
				head1[22] = 0xA2;
				head1[23] = 0;
				fixed (byte* h = head0)
				{
					s.func(s.context, h, head0.Length);
				}

				s.func(s.context, YTable, 64);
				stbiw__putc(s, 1);
				s.func(s.context, UVTable, 64);
				s.func(s.context, head1, 24);
				fixed (byte* d = &std_dc_luminance_nrcodes[1])
				{
					s.func(s.context, d, std_dc_chrominance_nrcodes.Length - 1);
				}

				fixed (byte* d = std_dc_luminance_values)
				{
					s.func(s.context, d, std_dc_chrominance_values.Length);
				}

				stbiw__putc(s, 0x10);
				fixed (byte* a = &std_ac_luminance_nrcodes[1])
				{
					s.func(s.context, a, std_ac_luminance_nrcodes.Length - 1);
				}

				fixed (byte* d = std_ac_luminance_values)
				{
					s.func(s.context, d, std_ac_chrominance_values.Length);
				}

				stbiw__putc(s, 1);
				fixed (byte* d = &std_dc_chrominance_nrcodes[1])
				{
					s.func(s.context, d, std_dc_chrominance_nrcodes.Length - 1);
				}

				fixed (byte* d = std_dc_chrominance_values)
				{
					s.func(s.context, d, std_dc_chrominance_values.Length);
				}

				stbiw__putc(s, 0x11);
				fixed (byte* a = &std_ac_chrominance_nrcodes[1])
				{
					s.func(s.context, a, std_ac_chrominance_nrcodes.Length - 1);
				}

				fixed (byte* d = std_ac_chrominance_values)
				{
					s.func(s.context, d, std_ac_chrominance_values.Length);
				}

				fixed (byte* h = head2)
				{
					s.func(s.context, h, head2.Length);
				}
			}

			{
				var fillBits = stackalloc ushort[2];
				fillBits[0] = 0x7F;
				fillBits[1] = 7;
				var imageData = (byte*)data;
				var DCY = 0;
				var DCU = 0;
				var DCV = 0;
				var bitBuf = 0;
				var bitCnt = 0;
				var ofsG = comp > 2 ? 1 : 0;
				var ofsB = comp > 2 ? 2 : 0;
				var x = 0;
				var y = 0;
				var pos = 0;
				var YDU = stackalloc float[64];
				var UDU = stackalloc float[64];
				var VDU = stackalloc float[64];

				for (y = 0; y < height; y += 8)
					for (x = 0; x < width; x += 8)
					{
						for (row = y, pos = 0; row < y + 8; ++row)
						{
							var clamped_row = row < height ? row : height - 1;
							var base_p = (stbi__flip_vertically_on_write != 0 ? height - 1 - clamped_row : clamped_row) *
										 width * comp;
							for (col = x; col < x + 8; ++col, ++pos)
							{
								float r = 0;
								float g = 0;
								float b = 0;
								var p = base_p + (col < width ? col : width - 1) * comp;
								r = imageData[p + 0];
								g = imageData[p + ofsG];
								b = imageData[p + ofsB];
								YDU[pos] = +0.29900f * r + 0.58700f * g + 0.11400f * b - 128;
								UDU[pos] = -0.16874f * r - 0.33126f * g + 0.50000f * b;
								VDU[pos] = +0.50000f * r - 0.41869f * g - 0.08131f * b;
							}
						}

						DCY = stbiw__jpg_processDU(s, &bitBuf, &bitCnt, YDU, fdtbl_Y, DCY, YDC_HT, YAC_HT);
						DCU = stbiw__jpg_processDU(s, &bitBuf, &bitCnt, UDU, fdtbl_UV, DCU, UVDC_HT, UVAC_HT);
						DCV = stbiw__jpg_processDU(s, &bitBuf, &bitCnt, VDU, fdtbl_UV, DCV, UVDC_HT, UVAC_HT);
					}

				stbiw__jpg_writeBits(s, &bitBuf, &bitCnt, fillBits[0], fillBits[1]);
			}

			stbiw__putc(s, 0xFF);
			stbiw__putc(s, 0xD9);
			return 1;
		}
	}
}