[//]:# (Microsoft YaHei UI)

·图片Mosaic（Photographic Mosaic）  
\-直接使用System.Drawing.Common NuGet包（为.NET Core重新生成的System.Drawing库）中的Bitmap类，故生成的图片像素需要满足：宽 × 高 × 3 ≤ 1.5GiB  
\-该版本代码为图片Mosaic中的Classic类型，即每个分割块都是相同的矩形  
\-该项目使用的是.NET Core 3最新测试|预览版本（Preview 5）运行库；语言标准为C# 8的β测试版本  
\-采用了多种位深的三原色采样（2~6位/色）作为图片指纹，最终选用深度为4位的版本，即采样颜色数量为(2^4)^3 = 4096个的版本  
\-自适应调整源图：整体直方图均衡化、最大分辨率化（允许的前提下尽可能逼近Bitmap容量上限，调整后有轻微的比例改变，宽|高 ± (分块宽|高 × 1像素)）  
\-灰度|黑白版本合并逻辑  
\-使用“原版”的余弦相似原理作为指纹的比对算法  
\-区分数字的使用方式：区分索引用数字、颜色值用数字；区分索引递增|递减用运算、正常运算；〇索引化数字、一索引化数字  
\-

·样例：  
//[来源](http://r0k.us/graphics/kodak)：Kodak发布，Rich  Franzen个人网站收藏  
\-原图：![原图](https://github.com/MetallicPickaxe/Photographic-Mosaic/blob/master/Read%20Me%E7%94%A8%E5%9B%BE/kodim20.png?raw=true)  
\-整体直方图均衡化：![]()  
\-8位色采样生成结果：![8位色采样生成结果](https://github.com/MetallicPickaxe/Photographic-Mosaic/blob/master/Read%20Me%E7%94%A8%E5%9B%BE/kodim20-Mosaic-8%E8%89%B2%E6%A0%A1%E9%AA%8C.png?raw=true)  
\-64位色采样生成结果：![64位色采样生成结果](https://github.com/MetallicPickaxe/Photographic-Mosaic/blob/master/Read%20Me%E7%94%A8%E5%9B%BE/kodim20-Mosaic-64%E8%89%B2%E6%A0%A1%E9%AA%8C.png?raw=true)  
\-512位色采样生成结果：![512位色采样生成结果](https://github.com/MetallicPickaxe/Photographic-Mosaic/blob/master/Read%20Me%E7%94%A8%E5%9B%BE/kodim20-Mosaic-512%E8%89%B2%E6%A0%A1%E9%AA%8C.png?raw=true)  
\-4096位色采样生成结果：![4096位色采样生成结果](https://github.com/MetallicPickaxe/Photographic-Mosaic/blob/master/Read%20Me%E7%94%A8%E5%9B%BE/kodim20-Mosaic-4096%E8%89%B2%E6%A0%A1%E9%AA%8C.png?raw=true)  
\-3 2768位色采样生成结果：![]()  
\-26 2144位色采样生成结果：![]()
