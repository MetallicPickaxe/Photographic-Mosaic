



#region Using
// using
// 自带
using System;
//
// 添加
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Encoder = System.Drawing.Imaging.Encoder;
// NuGet
using System.Drawing.Imaging;
using System.Collections.Generic;
#endregion

// Color.ToArgb()值结构：
// 字节方向：高 → 低
// 序号：		1		2								3		4			5		6				7		8
// 代表意义：透明值（Α|α，Alpha）		红（Red）		绿（Green）		蓝（Blue）

// 候选颜色空间：
// 底数		指数		幂										位数				名
// 2			8			256									3					灰度|黑白
// 2			9			512									3					
// 2			12		4096								4					
// 2			15		3 2768								5					
// 2			16		6 5536								5					6 5536色
// 2			18		26 2144							6					26万色
// 2			21		209 7152							7					
// 2			24		1677 7216						8					24位真·彩色|RGB|1600万色|1677 7216色
// 2			27		1 3421 7728					9					
// 2			30		10 7374 1824					10				10位深·彩色
// 2			32		42 9496 7296					10				ARGB（透明|α通道+RGB）
// 2			33		85 8993 4592					10				
// 2			36		687 1947 6736				11				12位深·彩色
// 2			39		5497 5581 3888				12				
// 2			42		4 3980 4651 1104			13				
// 2			45		35 1843 7208 8832			14				
// 2			48		281 4749 7671 0656		15				16位深·彩色

// 分为Classic、Chaos、Parquest
// 图片拼图片的学名叫蒙太奇效果（Montage）、Photomosaic（Photographic Mosaic）、蒙太奇照片、蒙太奇拼贴
// 来源：https://www.zhihu.com/question/23820935
namespace 图片Mosaic
{
	public class Classic版图片Mosaic
	{
		#region 构造器
		// 构造器
		//public Classic版图片Mosaic修改用(String 基路径_输入, Int32 取样压缩因数_输入 = 4, Int32 图像大小阈值_输入 = 16_1061_2736, Byte 填充边长_输入 = 100)
		public Classic版图片Mosaic(Int32 取样压缩因数_输入 = 4, Int32 图像大小阈值_输入 = 16_1061_2736, Int32 填充边长_输入 = 100)
		{
			填充边长 = 填充边长_输入;		// 推荐：100
														// 单位像素（Pixel）
														// 30靠猜，50能看，100能放大
														// 最低能保证填充用图片显示完整信息的条件是满足域内像素点数 ≥ 256个，即保证涵盖全灰度，即边长至少16（像素）；则3色的最小边长需要16^3，即4096是真·彩色的保障
			//
			//基路径 = 基路径_输入;		// 推荐：Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
													// 默认指向当前用户目录下的“图片”|“我的图片”
			//
			取样压缩因数 = 取样压缩因数_输入;		// 推荐：4
																		// 是取样个数的平方根，后者待变量化
																		// 候选值：1 ~ 8
			单色深度 = Convert.ToInt32(Math.Pow(2, 取样压缩因数));		// 候选值：2^1 = 2，2^2 = 4，2^3 = 8，2^4 = 16，2^5 = 32，2^6 = 64，2^7 = 128，2^8 = 256
																										// 结果值：2^3 = 8，4^3 = 64，8^3 = 512，16^3 = 4096，32^3 = 3 2768，64^3 = 26 2144，128^3 = 209 7152，256^3 = 1677 7216
																										// 指纹的色彩深度影响最终生成图目标大小，呈正相关
																										// ！太长，待简化
																										// ！若需要同时能实现黑白|灰度，则需要将3色通道的代码去除|合并
																										// 经测试，2^4 = 16即最终为4096色时肉眼观察效果最佳且时间合适
			单色钝化块大小 = 像素深度 / 单色深度;		// ！不能整除的情况会自动向下取整，未做防护
			//
			图像大小阈值 = 图像大小阈值_输入;		// ！仍是估算值，但距离真实值非常接近，亿位精准（17 0000 0000会过大报错）
																		// 常见图片比例的最大值：
																		// 1 : 1，23170 × 23170，向下取整
																		// 2 : 1，32768 × 16384，等于
																		// 4 : 3，26752 × 20064，向下取整
																		// 16 : 9，30880 × 17370，向下取整
		}
		#endregion

		#region 字段
		// 字段
		#region 常量
		// 常量|只读
		private const Int32 原色数 = 3;		// 3原色：红、绿、蓝
		private const Int32 偶数因子 = 2;		// 为求偶数用的“2”常量化
		private const Int32 位进制数 = 8;
		private readonly Point 原点 = default;		// 代表new Point(0, 0)，即赋值为点(0, 0)
																		// 这是1个标准点位，充当某种“default”
																		// 计算机的像素坐标体系与数学中的不同，虽实际操作仍是〇始，但不同于数学中视为原点（(0, 0)）用，而是对应其(1, 1)的ZeroIndexed()，故不可混淆
		private const Int32 颜色次序模 = 4;		// 满足1-Indexed值的减法以下映射：
																	// 1 → 3
																	// 2 → 2
																	// 3 → 1
		private const Int32 红色次序 = 1;
		private const Int32 绿色次序 = 2;
		private const Int32 蓝色次序 = 3;
		#endregion
		#region 变量
		// 变量
		// 构造器赋值
		private Int32 取样压缩因数 = default;
		public Int32 填充边长 = default;		// 正方形分割综合比例下效果较好|实现简单
		//private String 基路径 = default;
		private Int32 单色深度 = default;
		private Int32 单色钝化块大小 = default;
		private Int32 图像大小阈值 = default;
		#endregion
		#endregion

		#region 属性
		// 属性
		private Int32 像素深度 => OneIndexed(Byte.MaxValue);		// 二者完美契合，但前者赋予了更具体的意义
		private Int32 填充源数量 { get; set; }
		private Int32 红色移位掩码 => 位进制数 * 转换次序(红色次序);
		private Int32 绿色移位掩码 => 位进制数 * 转换次序(绿色次序);
		private Int32 蓝色移位掩码 => 位进制数 * 转换次序(蓝色次序);
		private Int32 红色掩码 => Byte.MaxValue << 红色移位掩码;
		private Int32 绿色掩码 => Byte.MaxValue << 绿色移位掩码;
		private Int32 蓝色掩码 => Byte.MaxValue << 蓝色移位掩码;
		private Int32 复颜色掩码 => 红色掩码 | 绿色掩码 | 蓝色掩码;
		#endregion

		#region 方法
		// 方法
		// 当前能“完美”处理的图片的大致分界线：300（像素），即：3 0000 ÷ 100，宽|高大于这个，实质上是在简单粗暴地压缩，即平滑|均值滤波；小于这个，则是对应的简单粗暴地插值
		public Image 生成(Image 源_输入, (List<Int32[]> 标识组, Image[] 填充源组) 填充源组_输入)
		{
			// 源合法性检测

			// 预处理
			源_输入 = 调整图片(源_输入);

			// 定义
			Image 目标图_输出 = new Bitmap(源_输入.Width, 源_输入.Height);
			Image 参考源 = default;
			Int32 目标索引 = default;
			Point 起点 = 原点;
			Size 大小 = new Size(填充边长, 填充边长);
			Point 索引 = 原点;

			// 处理
			// ！用分形的算法应该可以直接自由拼接，但需要考虑其形状与指定位置的色彩|亮度吻合程度，即Chaos版
			for( ; 索引.Y <= ZeroIndexed(目标图_输出.Height); 索引.Y += 大小.Height)
			{
				for(索引.X = default; 索引.X <= ZeroIndexed(目标图_输出.Width); 索引.X += 大小.Width)
				{
					// 预处理
					起点 = 索引;

					// 像素替换版
					// ！该版本需要解决Bitmap不可超过1.5GiB限制的问题
					//源色 = 源_输入.GetPixel(坐标索引.X, 坐标索引.Y);
					// 图片替换版
					参考源 = 获取图片区域(源_输入, 起点);		// ！Rectangle的构造不优雅，需要引入Point、Size类型

					// 对应序位算法，单色
					//目标索引 = ZeroIndexed(Convert.ToInt32(Math.Round(获取平均亮度(源色) / 复颜色空间 * 填充源组_输入.标识组.Length)));
					// 指纹算法
					目标索引 = 比较标识(生成标识(参考源), 填充源组_输入.标识组);		// 本身就是0-Indexed的值
																													// 备用：位图组[目标索引]的最小外接正方形化

					目标图_输出 = 绘制图片_核心(填充源组_输入.填充源组[目标索引], 目标图_输出, 起点);

					// 终处理
					参考源.Dispose();		// ？必要|可行否
				}
			}

			return 目标图_输出;
		}

		// 在不超过Bitmap()容量限制的前提下逼近之
		// 宽|高偶数化，便于计算
		// 宽|高为填充块倍数化，便于计算|减少末端缺损
		// 一般的源图不放大实现中现计算的图大小、分割坐标，节约内存，但不易精准操控宽|高末尾的边缘值
		public Image 调整图片(Image 源图_输入_输出)
		{
			// 定义
			Size 目标大小 = default;
			Int32 宽度容器 = default;		// 临时转接宽度用
			Int32 高度容器 = default;		// 临时转接高度用
			Decimal 宽高比 = Decimal.Divide(源图_输入_输出.Width, 源图_输入_输出.Height);		// 防止出现整除
			Boolean is过大 = default;

			// 赋值
			目标大小.Width = 宽度容器 = 源图_输入_输出.Width * 填充边长;
			目标大小.Height = 高度容器 = 源图_输入_输出.Height * 填充边长;

			// 预处理
			// 更大的图片不能由Bitmap直接加载，可行方案如下：
			// 1.FileStream()形式加载为Image，相当于使用流“压缩”了一下
			// 2.Memory Mapped File，理论上空间换时间|上限，DIY最佳选择
			// 3.3rd库
			// 4.？DirectX库
			// ！一说宽|高 ∈ (0, 3 2768)，由GDI+限制
			is过大 = Convert.ToUInt64(宽度容器) * Convert.ToUInt64(高度容器) * 原色数 > Convert.ToUInt64(图像大小阈值);

			// 使用Decimal增大精度，因其精度内是真实存储，“不丢值”
			// ！未考虑余数 < 填充图片大小太多如小于其一半画面的参差怎么处理，但交由了缩放模糊处理这种差异
			// ！只要修正了，多少变形一些
			// 以1-Indexed进行运算，为实际运算的值，更精确，而0-Indexed的值是有设计误差的
			// 使Bitmap类型的构造能在允许正常运行的范围内，防止报错
			if(is过大 == false)		// ∈ (0, 图像大小阈值]
			{
				while(Convert.ToUInt64(宽度容器) * Convert.ToUInt64(高度容器) * 原色数 <= Convert.ToUInt64(图像大小阈值))		// ？合并while()提取共通
				{
					// 计算新·目标大小
					目标大小.Width = 宽度容器;
					目标大小.Height = 高度容器;

					// 后置预处理
					// 后置保障由左侧得到左侧最逼近的宽|高
					宽度容器 += 填充边长;
					//
					高度容器 = Convert.ToInt32(Math.Round(目标大小.Width / 宽高比));
					高度容器 = 倍数化(高度容器, 填充边长);
				}
			}
			else		// ∈ (图像大小阈值, +∞)
			{
				while(Convert.ToUInt64(宽度容器) * Convert.ToUInt64(高度容器) * 原色数 > Convert.ToUInt64(图像大小阈值))		// 图片占用 ＞ 1.5GiB，即长 × 宽 × 3原色 ＞ 1.5GiB，按1原色+1像素占1字节算
																																															// 不能大于，可以等于
																																															// ！理论上可以直接 > 512MiB
																																															// 结果超出Int32范围，需要UInt64。其中的长宽之积在512MiB范围内，在Int32范围内
																																															// 全部是OneIndexed的值
				{
					// 前置预处理
					// 前置保障由右侧得到左侧最逼近的宽|高
					宽度容器 -= 填充边长;
					//
					高度容器 = Convert.ToInt32(Math.Round(目标大小.Width / 宽高比));		// 高度_新 = 宽度_新 ÷ 宽高比，宽高比 = 宽度 ÷ 高度
					高度容器 = 倍数化(高度容器, 填充边长);

					// 计算新·目标大小
					目标大小.Width = 宽度容器;
					目标大小.Height = 高度容器;
				}
			}

			// 进行缩放
			源图_输入_输出 = 缩放图形(源图_输入_输出, 目标大小);

			return 源图_输入_输出;
		}

		#region 比较标识()
		// 计算偏离情况
		// 实际1st参数是单个参数；2nd参数是一组参数，但表示上以数组形式出现，同时为了避免2维数组的误导性|干扰性，选用了List作外层
		private Int32 比较标识(Int32[] 参照信息_输入, List<Int32[]> 候选信息组_输入)
		{
			// 定义
			Decimal 差 = Decimal.MinValue;		// 设为〇值会导致判断的首次即合理，需要避开，而别的方法不够优雅|需要创建额外处理逻辑
																	// 且这个差值无法达到，但整体还不够优雅，最好是设计上能够避开
			Decimal 当前差 = default;
			Boolean is适合 = default;
			Int32 索引_输出 = default;

			for(Int32 索引 = Next(default); 索引 <= ZeroIndexed(候选信息组_输入.Count); 索引++)
			{
				当前差 = 比较标识_核心(参照信息_输入, 候选信息组_输入[索引]);

				is适合 = (当前差 > 差) ? true : false;		// 与上1个最适合差的接近程度比较：接近1为相似
																			// ！更适合使用do-while

				if(is适合)
				{
					// 下次循环的预处理
					差 = 当前差;		// 差放在这里赋值能保障last值的纯净
					索引_输出 = 索引;		// 兼任了last也没有匹配上的处理
				}
				else
				{
					//break;
				}
			}

			return 索引_输出;		// ？应该不会出现未命中以default值返回而不是真·〇返回的情况
		}

		// 兼容修正·余弦相似定理、原版·余弦相似定理
		private Decimal 比较标识_核心(Int32[] 参照值_输入, Int32[] 候选值_输入)
		{
			// 简单版本
			//UInt64 离差 = Convert.ToUInt64(Math.Abs(Convert.ToInt64(候选值_输入) - Convert.ToInt64(参照值_输入)));

			// 流行的复杂版本
			Int32 长度 =参照值_输入.Length;		// ！理论上：参照值长度、候选值长度相等，都可行
			Decimal 标识_输出 = default;
			Int64 内积和 = default;
			Int64 参照值平方和 = default;
			Int64 候选值平方和 = default;

			for(Int32 索引 = default; 索引 <= ZeroIndexed(长度); 索引++)
			{
				内积和 += 参照值_输入[索引] * 候选值_输入[索引];
				参照值平方和 += 参照值_输入[索引] * 参照值_输入[索引];
				候选值平方和 += 候选值_输入[索引] * 候选值_输入[索引];
			}

			// 标准化：防止〇除|〇被除数
			//内积和 = 计算时标准化(内积和);		// ！是否允许〇-被除数
			参照值平方和 = 除法计算时标准化(参照值平方和);
			候选值平方和 = 除法计算时标准化(候选值平方和);

			// 余弦相似性
			标识_输出 = 内积和 / Convert.ToDecimal(Math.Sqrt(参照值平方和) * Math.Sqrt(候选值平方和));

			//return 离差;
			return 标识_输出;
		}
		#endregion

		#region 绘制()
		#region 应用
		public Image 获取图片区域(Image 源图_输入, Point 源起点_输入)
		{
			//Bitmap 目标图_输出 = 源图_输入.Clone(获取域_输入, PixelFormat.Format32bppArgb);		// 官方推荐版

			Size 源大小 = new Size(填充边长, 填充边长);		// 大小不需要0-Indexed值
			Rectangle 源域 = new Rectangle(源起点_输入, 源大小);
			Point 目标起点 = 原点;
			Size 目标大小 = 源大小;		// 大小不需要ZeroIndexed0-Indexed值
													// 将指定大小的区域复制下来
			Rectangle 目标域 = new Rectangle(目标起点, 目标大小);

			return 绘制图片_核心(源图_输入, 源域, 目标域);
		}

		// ？比Bitmap()的承载力强
		// 比Bitmap()的选项多
		private Image 缩放图形(Image 源图_输入, Size 目标大小_输入)
		{
			Point 源起点 = 原点;
			Size 源大小 = new Size(源图_输入.Width, 源图_输入.Height);		// 大小不需要0-Indexed值
			Rectangle 源域 = new Rectangle(源起点, 源大小);
			Point 目标起点 = 原点;
			Rectangle 目标域 = new Rectangle(目标起点, 目标大小_输入);

			return 绘制图片_核心(源图_输入, 源域, 目标域);
		}

		// 保持原比例填充为其最小外接正方形的策略会导致背景过多，使整体图片呈现灰度效果		×
		// 保持原比例填充为其最大内切正方形的策略会导致非中心部分的内容不完整|缺失				√
		// 默认策略是强制缩放为指定大小的正方形，会导致长宽比失调											×
		// ！图系坐标0-Indexed值
		private Image 正方形化(Image 源图_输入)
		{
			// 预处理
			源图_输入 = 长度偶数化(源图_输入);
			Size 大小 = new Size(源图_输入.Width, 源图_输入.Height);		// 大小不需要0-Indexed
			Point 源起点 = 设定起点(大小);

			// 定义+赋值
			Int32 源边长 = Min(大小.Width, 大小.Height);		// 外接时为Max()
			Point 目标起点 = 原点;
			Int32 的边长 = 源边长;		// 外接时为Max()
			Size 源大小 = new Size(的边长, 的边长);		// 大小不需要0-Indexed
			Size 目标大小 = 源大小;
			Rectangle 源域 = new Rectangle(源起点, 源大小);
			Rectangle 目标域 = new Rectangle(目标起点, 目标大小);

			return 绘制图片_核心(源图_输入, 源域, 目标域);
		}

		private Image 长度偶数化(Image 源图_输入)
		{
			Size 目标大小 = new Size(偶数化(源图_输入.Width), 偶数化(源图_输入.Height));

			return 缩放图形(源图_输入, 目标大小);
		}
		#endregion
		#region 核心
		private Image 绘制图片_核心(Image 源图_输入, Rectangle 源域_输入, Rectangle 目标域_输入)
		{
			Image 目标图 = new Bitmap(目标域_输入.Size.Width, 目标域_输入.Size.Height);		// ？没有Size版本的

			return 绘制图片_核心(源图_输入, 目标图, 源域_输入, 目标域_输入);
		}

		private Image 绘制图片_核心(Image 源图_输入, Image 目标图_输入_输出, Point 目标起点_输入)
		{
			Point 源起点 = 原点;
			Size 源大小 = new Size(源图_输入.Width, 源图_输入.Height);		// 大小不需要0-Indexed
																											// 实与填充边长同值，但如此调用更贴近意义
			Rectangle 源域 = new Rectangle(源起点, 源大小);
			Size 目标大小 = new Size(填充边长, 填充边长);
			Rectangle 目标域 = new Rectangle(目标起点_输入, 目标大小);

			return 绘制图片_核心(源图_输入, 目标图_输入_输出, 源域, 目标域);
		}

		private Image 绘制图片_核心(Image 源图_输入, Image 目标图_输入_输出, Rectangle 源域_输入, Rectangle 目标域_输入)		// ！传递Bitmap对象实体还是文件路径待考虑
		{
			// 定义
			Graphics 生成器 = default;

			// 赋值
			// 设置生成器
			生成器 = Graphics.FromImage(目标图_输入_输出);
			// 背景色
			//生成器.Clear(Color.White);		// 透明背景增大黑色感，白色效果更好
															// 比FillRectangle()|FillRegion()省事
			// 绘制质量
			生成器.CompositingQuality = CompositingQuality.HighQuality;		// 合成质量
			生成器.SmoothingMode = SmoothingMode.HighQuality;		// 平滑模式质量
			生成器.InterpolationMode = InterpolationMode.HighQualityBicubic;		// 插值算法质量

			// 处理
			生成器.DrawImage(源图_输入, 目标域_输入, 源域_输入, GraphicsUnit.Pixel);		// ！默认的填充方式是拉伸至正方形，除了比例乱了没啥大事

			// 终处理
			//源图_输入.Dispose();
			生成器.Dispose();

			return 目标图_输入_输出;
		}
		#endregion
		#endregion

		#region 统计像素信息()
		#region 标识
		// 亦应定名为：生成标识_核心()
		private (Int32 红, Int32 绿, Int32 蓝) 生成标识_核心(Color 颜色_输入)
		{
			(Int32 红, Int32 绿, Int32 蓝) 颜色 = default;
			Int32 色 = 颜色_输入.ToArgb() & 复颜色掩码;

			// ！此部分处理需要提取函数
			颜色.红 = OneIndexed((色 & 红色掩码) >> (位进制数 * 红色移位掩码));
			颜色.绿 = OneIndexed((色 & 绿色掩码) >> (位进制数 * 绿色移位掩码));
			颜色.蓝 = OneIndexed((色 & 蓝色掩码) >> (位进制数 * 蓝色移位掩码));

			return 颜色;
		}

		// 像素→图片，是生成标识的输入参数前置“取齐”处理
		private Int32[] 生成标识(Color 颜色_输入)		// ！需要更改为使用绘制()的版本
		{

			Int32[] 标识_输出 = default;
			SolidBrush 绘制器 = new SolidBrush(颜色_输入);
			Int32 取样用图边长 = 填充边长 / 取样压缩因数;		// ！不能整除的情况会自动向下取整，有精度损失，未做防护
			Size 大小 = new Size(取样用图边长, 取样用图边长);
			Image 源位图 = new Bitmap(大小.Width, 大小.Height);		// ？没有Size版本
			Graphics 生成器 = Graphics.FromImage(源位图);

			// 设定背景色
			生成器.Clear(颜色_输入);

			// 设置绘制质量
			生成器.CompositingQuality = CompositingQuality.HighQuality;		// 合成质量
			生成器.SmoothingMode = SmoothingMode.HighQuality;		// 平滑模式质量
			生成器.InterpolationMode = InterpolationMode.HighQualityBicubic;		// 插值算法质量

			Rectangle 绘制域 = new Rectangle(default, default, 源位图.Width, 源位图.Height);

			生成器.FillRectangle(绘制器, 绘制域);

			生成器.Dispose();

			标识_输出 = 生成标识(源位图);

			return 标识_输出;
		}

		// 核心算法
		// 日本的大津算法缺点：小前景、小差别、噪声、前景|背景剧烈变化下不佳
		// 适用原版·余弦相似定理，不含均值的计算
		// 彩色算法思路来源：https://bbs.luobotou.org/thread-12273-1-1.html
		// 以颜色值为码位，以像素数为码值
		private Int32[] 生成标识(Image 源图_输入)
		{
			// 定义
			//Int32 长度 = Convert.ToInt32(Math.Pow(单色深度, 原色数));
			Int32 长度 = 单色深度 * 单色深度 * 单色深度;
			Int32[ , , ] 标识容器 = new Int32[单色深度, 单色深度, 单色深度];
			Int32[] 标识_输出 = new Int32[长度];
			Point 坐标索引 = default;
			Size 填充大小 = new Size(填充边长, 填充边长);
			Color 像素 = default;
			Bitmap 源图 = new Bitmap(源图_输入);
			Int32 索引 = default;

			// 颜色种类钝化取样
			for( ; 坐标索引.Y <= ZeroIndexed(填充大小.Height); 坐标索引.Y++)
			{
				for(坐标索引.X = default; 坐标索引.X <= ZeroIndexed(填充大小.Width); 坐标索引.X++)
				{
					像素 = 源图.GetPixel(坐标索引.X, 坐标索引.Y);		// ？没有Point版本

					标识容器[获取钝化颜色(像素.R), 获取钝化颜色(像素.G), 获取钝化颜色(像素.B)]++;
				}
			}

			// 降维：3 → 1
			foreach(Int32 颜色值 in 标识容器)		// ！能等同于3层for()手动遍历的顺序，虽自行保持一致不影响比对，但需要考证其是否一致
			{
				标识_输出[索引] = 颜色值;

				索引++;
			}

			return 标识_输出;
		}
		#endregion
		#region 亮度
		// 2^8 → 2^X
		// 0-Indexed值 → 0-Indexed值
		private Int32 获取钝化颜色(Int32 颜色值_输入)
		{
			Int32 钝化颜色值_输出 = Int32.MinValue;		// 异常值

			颜色值_输入 = OneIndexed(颜色值_输入);

			// “自动化”生成的if()
			// ！改为switch()更合适
			for(Int32 索引 = OneIndexed(default); 索引 <= 单色深度; 索引++)		// ∈ (0, 单色深度]
			{
				if(颜色值_输入 <= 单色钝化块大小 * 索引)		// ∈ (0, 单色钝化块大小 * 索引] ~ (单色钝化块大小 * 索引, 256]
																					// 单色钝化块大小 = 256 ÷ 单色深度
				{
					钝化颜色值_输出 = 索引;

					break;		// 防止多次赋值，原if()没有，for()化后因需要而增设
				}
				else
				{
					// 占位
					//continue;		// ！有无都一样
				}
			}

			// 1-Indexed值计算，0-Indexed值使用
			return ZeroIndexed(钝化颜色值_输出);		// 目前主要用于索引创建
		}

		private Decimal 获取平均亮度(Image 源图_输入) => 获取平均亮度(获取平均颜色(源图_输入));

		// 也是亮度（Luminance），也是灰度（Gray）
		// 这是1-Indexed的各颜色分量进行的运算，公式也是1-Indexed的
		// 亦可称为：生成标识()
		private Decimal 获取平均亮度((Int32 红, Int32 绿, Int32 蓝) 源色_输入)
		{
			// 简易算法：3原色值取算术平均
			//return (OneIndexed(源色_输入.R) + OneIndexed(源色_输入.G) + OneIndexed(源色_输入.B)) / 3);

			// 经典算法：NTSC（National Television Standards Committee）电视制式的色彩空间YIQ中的Y（Luminance）
			// 来源：https://stackoverflow.com/questions/596216/formula-to-determine-brightness-of-rgb-color
			return (0.2126M * OneIndexed(源色_输入.红) + 0.7152M * OneIndexed(源色_输入.绿) + 0.0722M * OneIndexed(源色_输入.蓝));		// 标准版，对应某种颜色空间
																																																					// 1063 : 3576 : 361，3者和为5000
			//return (0.299M * 源色_输入.R + 0.587M * 源色_输入.G + 0.114M * 源色_输入.B);		// 适用人感知的版本
			//return Math.Pow( 0.299M * Math.Pow(源色_输入.R, 2) + 0.587 * Math.Pow(源色_输入.G, 2) + 0.114M * Math.Pow(源色_输入.B, 2), 0.5M);		// √(0.299 * R^2 + 0.587 * G^2 + 0.114 * B^2)
																																																									// 适用人感知的版本，计算上更慢
			//
			// 扩展版
			//return Convert.ToUInt64(Math.Round((0.2126M * 源色_输入.红 + 0.7152M * 源色_输入.绿 + 0.0722M * 源色_输入.绿) * 映射比));		// 将 ∈ [1, 256]的亮度比映射到 ∈ [1, 绘制源数量]上面

			// “遵循自然”的算法
			//return 源色_输入.GetBrightness() * 像素深度;		// 首先，这是个比率，不是均值；其次，内部实现非常粗糙
																						// 因其是比率，故算“度值”的话需要先还原，即 × 像素深度
																						// ！这是0-Indexed下进行的计算，有设计误差
		}

		private (Int32 红, Int32 绿, Int32 蓝) 获取平均颜色(Image 源图_输入)
		{
			Int64 像素空间 = 源图_输入.Width * 源图_输入.Height;
			(Int32 红, Int32 绿, Int32 蓝) 颜色_输出 = default;

			// 平均化
			// 求整图的RGB通道平均颜色，算术平均，可换为更高级平均算法
			颜色_输出 = Divide(统计颜色(源图_输入), 像素空间);

			//return OneIndexed(容器.ToArgb() & 复颜色掩码);
			//
			//容器 = Color.FromArgb(Convert.ToInt32(ZeroIndexed(Convert.ToUInt64(Math.Round(红色计数 * 1M / 像素空间)))), Convert.ToInt32(ZeroIndexed(Convert.ToUInt64(Math.Round(绿色计数 * 1M / 像素空间)))), Convert.ToInt32(ZeroIndexed(Convert.ToUInt64(Math.Round(蓝色计数 * 1M / 像素空间)))));
			//return 钝化颜色深度(容器);		// ！不非得有
			//return 容器;

			return 颜色_输出;
		}

		// 理论上只用于各分量，以匿名|值元组类型是便于分量的次序化标识
		private (Int32 红, Int32 绿, Int32 蓝) 统计颜色(Image 源图_输入)
		{
			(Int32 红, Int32 绿, Int32 蓝) 颜色计数_输出 = default;
			Point 索引 = 原点;
			(Int32 红, Int32 绿, Int32 蓝) 容器 = default;
			Bitmap 源图 = new Bitmap(源图_输入);

			for( ; 索引.Y <= ZeroIndexed(源图_输入.Height); 索引.Y++)
			{
				for(索引.X = default; 索引.X <= ZeroIndexed(源图_输入.Width); 索引.X++)
				{
					容器 = 生成标识_核心(源图.GetPixel(索引.X, 索引.Y));		// ？没有Point版本

					颜色计数_输出 = Add(颜色计数_输出, 容器);
				}
			}

			// 终处理
			//源图_输入.Dispose();		// ？可行

			return 颜色计数_输出;
		}
		#endregion
		#endregion

		// 与获取版的区别在于上层的额外操作如排序等归为此处，暂空
		public (List<Int32[]>, Image[]) 生成填充源(String 填充源路径_输入)
		{
			// 定义
			(List<Int32[]> 标识组, Image[] 填充源组) 填充源_输出 = 获取填充源(填充源路径_输入);
			//Decimal[] 亮度值组 = new Decimal[绘制源数量];

			// 处理
			// 排序
			// 指纹算法无法简单排序，故弃用

			return 填充源_输出;
		}

		// 以压缩后的Thumb取样
		// 裁剪也算精度损失，而且“清晰”状态下的“代表值”和处理后的实际数据是有偏差的，故考虑直接在此处理完成，减少压力
		public (List<Int32[]>, Image[]) 获取填充源(String 填充源路径_输入)
		{
			// ！输入合法性检测

			// 预处理
			// ！还有1种DirectoryInfo → FileInfo[] → .FullName（String）的方式，待比较
			String[] 图名组 = Directory.GetFiles(填充源路径_输入, $@"*.jpg", SearchOption.TopDirectoryOnly);		// 仅搜索当前目录
			//String[] 图名组 = Directory.GetFiles(绘制源路径_输入,ImageFormat.Png.ToString().ToLower(), SearchOption.TopDirectoryOnly);		// 仅搜索当前目录
																																																				// ！不够优雅
																																																				// ！不同类型源无法切换、无法兼容，考虑用Image
			填充源数量 = 图名组.Length;

			// 定义
			List<Int32[]> 填充源信息组_输出 = new List<Int32[]>();
			Image[] 填充源组_输出 = new Image[填充源数量];
			Size 目标大小 = new Size(填充边长, 填充边长);

			// 修正颜色空间
			//自适应颜色空间(长度);

			for(Int32 索引 = default; 索引 <= ZeroIndexed(填充源数量); 索引++)
			{
				// ！图片是否能打开|文件名是否合法|存在未判定
				// ！不能打开是跳过还是报错还是空出来还是填充默认|替补图片未确定

				填充源组_输出[索引] = new Bitmap(图名组[索引]);

				// 标准化|裁剪
				// ！可考虑引入OpenCV等库针对人脸进行剪裁
				// 缩放|格式化后再计算
				填充源组_输出[索引] = 正方形化(填充源组_输出[索引]);
				// 压缩
				填充源组_输出[索引] = 缩放图形(填充源组_输出[索引], 目标大小);

				// 创建标识
				填充源信息组_输出.Add(生成标识(填充源组_输出[索引]));
			}

			return (填充源信息组_输出, 填充源组_输出);
		}

		// 一般+的算法
		// 灰度高占比的单项 → 多灰度阶层，即纵向改横向，即提高对比度，尽量更多突出各部分
		// 主要面向色彩集中的图，可选
		// 来源：？https://github.com/defineYIDA/picequalization
		public Image 直方图均衡化(Image 源图_输入)		// ！待重构
																				// ！3原色颜色分度的处理代码需要提取函数
		{
			Int64 像素空间 = 源图_输入.Width * 源图_输入.Height;
			(Int32[] 红, Int32[] 绿, Int32[] 蓝) 亮度组 = (new Int32[像素深度], new Int32[像素深度], new Int32[像素深度]);
			(Decimal[] 红, Decimal[] 绿, Decimal[] 蓝) 亮度密度组 = (new Decimal[像素深度], new Decimal[像素深度], new Decimal[像素深度]);
			Bitmap 目标图_输出 = new Bitmap(源图_输入.Width, 源图_输入.Height);
			(Decimal 红, Decimal 绿, Decimal 蓝) 容器 = default;
			Color 像素 = default;
			Point 坐标索引 = 原点;
			Bitmap 源图 = new Bitmap(源图_输入);

			// 〇始部分
			for( ; 坐标索引.Y <= ZeroIndexed(源图_输入.Height); 坐标索引.Y++)
			{
				for(坐标索引.X = default; 坐标索引.X <= ZeroIndexed(源图_输入.Width); 坐标索引.X++)
				{
					像素 = 源图.GetPixel(坐标索引.X, 坐标索引.Y);

					// 计算各颜色分度值的数量集
					亮度组.红[像素.R]++;
					亮度组.绿[像素.G]++;
					亮度组.蓝[像素.B]++;
				}
			}

			// 计算各颜色值的占比
			for(Int32 索引 = default; 索引 <= ZeroIndexed(像素深度); 索引++)
			{
				亮度密度组.红[索引] = Decimal.Divide(亮度组.红[索引], 像素空间);
				亮度密度组.绿[索引] = Decimal.Divide(亮度组.绿[索引], 像素空间);
				亮度密度组.蓝[索引] = Decimal.Divide(亮度组.蓝[索引], 像素空间);
			}

			// 计算累积百分比
			// ？哪个著名的变换来着
			// ！不是一始，而是防止计算时下限越界，利用这种方法，化简了起始索引的计算：亮度密度组.XXX[default] += 0
			for(Int32 索引 = Next(default); 索引 <= ZeroIndexed(像素深度); 索引++)
			{
				// 向前取，0-Indexed下1始，防止超过上限
				亮度密度组.红[索引] += 亮度密度组.红[Previous(索引)];
				亮度密度组.绿[索引] += 亮度密度组.绿[Previous(索引)];
				亮度密度组.蓝[索引] += 亮度密度组.蓝[Previous(索引)];
			}

			for(坐标索引.Y = default; 坐标索引.Y <= ZeroIndexed(源图_输入.Height); 坐标索引.Y++)
			{
				for(坐标索引.X = default; 坐标索引.X <= ZeroIndexed(源图_输入.Width); 坐标索引.X++)
				{
					像素 = 源图.GetPixel(坐标索引.X, 坐标索引.Y);		// ！没有Point版本

					容器.红 = 像素.R;
					容器.绿 = 像素.G;
					容器.蓝 = 像素.B;

					if(容器.红 == default)
					{
						//容器.红 = default;		// 保持〇亮度映射得也是〇
					}
					else
					{
						// 容器.红 = 源亮度 × 累计百分比
						容器.红 = 亮度密度组.红[Convert.ToInt32(Math.Round(容器.红))] * 像素深度;
					}

					if(容器.绿 == default)
					{
						//容器.绿 = default;		// 保持〇亮度映射得也是〇
					}
					else
					{
						// 容器.绿 = 源亮度 × 累计百分比
						容器.绿 = 亮度密度组.绿[Convert.ToInt32(Math.Round(容器.绿))] * 像素深度;
					}

					if(容器.蓝 == default)
					{
						//容器.蓝 = default;		// 保持〇亮度映射得也是〇
					}
					else
					{
						// 容器.蓝 = 源亮度 × 累计百分比
						容器.蓝 = 亮度密度组.蓝[Convert.ToInt32(Math.Round(容器.蓝))] * 像素深度;
					}

					像素 = Color.FromArgb(ZeroIndexed(Convert.ToInt32(容器.红)), ZeroIndexed(Convert.ToInt32(容器.绿)), ZeroIndexed(Convert.ToInt32(容器.蓝)));		// 此处可切换为执行灰度化

					目标图_输出.SetPixel(坐标索引.X, 坐标索引.Y, 像素);		// 简化了对红色分度值、绿色分度值、蓝色分度值 = default的颜色的处理
																									// ！使用BitmapData的性能更好
																									// ！没有Point版本

					// 终处理
					容器 = default;		// 1代3，nice！
				}
			}

			// 终处理
			源图_输入.Dispose();

			return 目标图_输出;
		}

		// 1-Indexed值→0-Indexed值
		// 最大内切正方形的起点
		private Point 设定起点(Size 源大小)
		{
			Boolean is横向图 = ((源大小.Width >= 源大小.Height) ? true : false);		// 前者含正方形
																														// ？与Max()可合并
			Int32 宽高差 = default;		// 一定是偶数
													// ∈ [0, +∞)
			Point 起点_输出 = 原点;

			// 设置绘制域
			// 外接时计算主要在的绘制域；内切主要在源绘制域
			// 源
			// 确定目标起点坐标
			if(is横向图)		// ！考虑将纵向图转置为横向图进行处理，代码复用上更佳
									// ！待优雅
			{
				宽高差 = 源大小.Width - 源大小.Height;

				// 左顶格，起点_输出.X = default，不用再赋值

				if(宽高差 == default)		// 等宽
				{
					// 占位
					//起点_输出.X = default;
				}
				else
				{
					起点_输出.X = 宽高差 / 2;		// ！直接在数值上执行了ZeroIndexed() + Next()，需要拆分
																// ？需要常量化
				}
			}
			else		// 纵向图
			{
				宽高差 = 源大小.Height - 源大小.Width;

				if(宽高差 == default)		// 理论上不存在
				{
					// 占位
					//起点_输出.Y = default;
				}
				else
				{
					起点_输出.Y = 宽高差 / 2;		// ！直接在数值上执行了ZeroIndexed() + Next()，需要拆分
																// ？需要常量化
				}

				// 上顶格，起点_输出.Y = default，不用再赋值
			}

			return 起点_输出;
		}

		#region 工具
		// 工具
		private Int32 RoundDivide(Int32 被除数_输入, Int64 除数_输入) => Convert.ToInt32(Math.Round(Decimal.Divide(被除数_输入, 除数_输入)));
		private (Int32 红, Int32 绿, Int32 蓝) Add((Int32 红, Int32 绿, Int32 蓝) 被加数_输入, (Int32 红, Int32 绿, Int32 蓝) 加数_输入) => (被加数_输入.红 + 加数_输入.红, 被加数_输入.绿 + 加数_输入.绿, 被加数_输入.蓝 + 加数_输入.蓝);
		private (Int32 红, Int32 绿, Int32 蓝) Divide((Int32 红, Int32 绿, Int32 蓝) 被除数_输入, Int64 除数_输入)
		{
			(Int32 红, Int32 绿, Int32 蓝) 商_输出 = default;

			商_输出.红 = RoundDivide(被除数_输入.红, 除数_输入);
			商_输出.绿 = RoundDivide(被除数_输入.绿, 除数_输入);
			商_输出.蓝 = RoundDivide(被除数_输入.蓝, 除数_输入);

			return 商_输出;
		}
		//
		private Int32 转换次序(Int32 源次序_输入) => ZeroIndexed(颜色次序模 - 源次序_输入);
		//
		private Int32 倍数化(Int32 被除数_输入_输出, Int32 除数_输入)		// ！参数列表不合理，需要重构，参考Math.Ceiling()的输入、输出形式
		{
			Int32 余数 = 被除数_输入_输出 % 除数_输入;

			if(余数 != default)
			{
				被除数_输入_输出 = 被除数_输入_输出 - 余数 + 除数_输入;		// 被除数整除数倍化
			}
			else
			{
				// 占位
			}

			return 被除数_输入_输出;
		}
		private Int32 偶数化(Int32 源_输入) => 源_输入 + ((源_输入 % 偶数因子 == default) ? 0 : 1);
		//
		// 逻辑同Math.Min()，1st位兼任小于、等于
		private Int32 Min(Int32 被比数_输入, Int32 比较数_输入) => (被比数_输入 <= 比较数_输入) ? 被比数_输入 : 比较数_输入;
		// 逻辑同Math.Max()，1st位兼任大于、等于
		private Int32 Max(Int32 被比数_输入, Int32 比较数_输入) => (被比数_输入 >= 比较数_输入) ? 被比数_输入 : 比较数_输入;
		//
		private Int64 除法计算时标准化(Int64 源值_输入)
		{
			if(源值_输入 == default)
			{
				return 1;		// 强调是1而不是Next()|OneIndexed()的产物，也不是−1、0.1等接近〇的数值
									// 在乘除运算尤其是除法运算中有特殊价值，或者叫〇在除法中的最近代替
			}
			else
			{
				return 源值_输入;
			}
		}
		//
		private Int32 OneIndexed(Int32 ZeroIndexed_输入) => Next(ZeroIndexed_输入);
		private Int32 ZeroIndexed(Int32 OneIndexed_输入) => Previous(OneIndexed_输入);
		//
		private Int32 Previous(Int32 源数_输入) => Convert.ToInt32(Previous(Convert.ToUInt64(源数_输入)));
		private UInt64 Previous(UInt64 源数_输入)
		{
			if(源数_输入 != default)
			{
				return 源数_输入 - 1;
			}
			else		// 主要用于统计后颜色值的索引转换处
			{
				return default;
			}
		}
		private Int32 Next(Int32 源数_输入) => 源数_输入 + 1;
		//
		// 压缩并写入
		private void 写入图片(Image 源图_输入, String 路径_输入, String 类型_输入)		// ！类型_输入应该用更合适的类型传输
																																// ！编码器的类型和图片路径的类型重复了，考虑复用，否则现在缺少相同类型的输入合法性检测
																																// ！是否编码目标大小没有区别
		{
			// 编码器设置
			ImageCodecInfo 编码 = default;
			foreach(ImageCodecInfo 编码信息 in ImageCodecInfo.GetImageEncoders())
			{
				if(编码信息.FormatDescription == 类型_输入.ToUpper())		// ！FormatDescription不一定准确，且不一定和类型名一致，如：JPG、JPEG
				{
					编码 = 编码信息;
				}
				else
				{
					// 占用
				}
			}

			// 质量设置
			EncoderParameters 编码参数 = new EncoderParameters();
			编码参数.Param[default] = new EncoderParameter(Encoder.Quality, 100);		// ！100的硬编码需要处理

			源图_输入.Save(路径_输入, 编码, 编码参数);		// 编码器涵盖了的文件类型
		}
		#endregion
		#endregion
	}
}