# 注意事项

项目基于`dotnet 6.0`构建，可自行更换到其他TargetFramework

# 开篇说明

指罪吧汉化版本，并非无名汉化
本文可能存在纰漏，如有错误欢迎指正

# 存档的“单向兼容”

用过汉化的朋友们可能会注意到，汉化版的存档后缀名发生了变化，即变为了`.sv`

并且，原版存档可以改个后缀直接在汉化版使用，但是反过来却不行，游戏甚至会直接卡死

这是为什么？下面进行分析

# 逆向分析

## 游戏的存档/读档逻辑

下面是游戏存档时的操作：

```c++
// from GTAmodding/revc 
// src/save/PCSave.cpp

// 定义存档文件名,其中".b"就是后缀
sprintf(savename, "%s%i%s", DefaultPCSaveFileName, i + 1, ".b");
//打开对应文件，准备写入
int file = CFileMgr::OpenFile(savename, "rb");
//下略
```

可以看到，`savename`最终是`.b`结尾的

同理，读档的时候也是这样，并且**只扫描使用该后缀的文件**

## 汉化补丁的相关修改

这是`gta_cn.asi`的某个函数，实现了对`b`到`sv`的替换：

```cpp
void *__cdecl sub_10001FD0(int a1)
{
  void *result; // eax

  if ( a1 )
  {
    if ( !dword_100168C4 )
    {
      memcpy(&unk_1001696C, (const void *)0x6D863C, 4u);
      memcpy(&unk_100168D4, (const void *)0x6D8AB8, 4u);
      memcpy(&unk_10016970, (const void *)0x6D8AC8, 4u);
    }
    //程序正常运行的情况下会执行该分支,即对内存操作，修改字符
    strcpy((char *)0x6D863C, ".sv");
    strcpy((char *)0x6D8AB8, ".sv");
    result = strcpy((char *)0x6D8AC8, ".sv");
  }
  else
  {
    memcpy((void *)0x6D863C, &unk_1001696C, 4u);
    memcpy((void *)0x6D8AB8, &unk_100168D4, 4u);
    result = memcpy((void *)0x6D8AC8, &unk_10016970, 4u);
  }
  return result;
}
```

所以，原版与汉化会互相忽略掉对方的存档

那为什么原版的可以给汉化用，反过来就崩溃？因为文件内容有改动，细看下节

## 存档的二进制内容

### 存档名

根据 [Saves (GTA VC) - GTAMods Wiki](https://gtamods.com/wiki/Saves_(GTA_VC)) 可以了解到

文件中，`[0x0004,0x0033]`这长为`48`字节的区域为`存档名`

如果你对`reVC`有一定了解，你可以在`src\save\GenericGameStorage.cpp`中看到这部分的文件结构与操作逻辑

可能是出于日本市场的原因，R*在游戏中提供了Unicode的支持，[GXT/Japanese - GTAMods Wiki](https://gtamods.com/wiki/GXT/Japanese) 中给出的字符表似乎就是`SHIFT_JIS`编码表

可以看到存档的名字其实是一个`wchar`类型的数组，这与存档中**每`2`个字节表示一个字符**相符合

```c
// 最后一次完成的任务名
wchar *lastMissionPassed;
// 存档名
wchar saveName[24];
// 后缀，其实反映到游戏里就是存档名后面的"..."
wchar suffix[6];
```

接下来是存档名的写入过程，在这之前，你可能需要了解一下什么是GXT

> GXT格式是一种加密的文本，通过类似于字典(Key - Value)的方式，实现了游戏文本的映射，其初衷是方便游戏的本地化工作
>
> 可由类似如下的形式表达
>
> ```
> [ITBEG]
> In the beginning
> [IN_VEH]
> 呃我忘了是什么了，反正就是让你回到车上
> ```
>
> 也就是说 ` TheText.Get("ITBEG") == "In the beginning"`
>
> 想深入了解可以查阅 [GXT - GTAMods Wiki](https://gtamods.com/wiki/GXT)

```c
// 如果完成过任务，则获取最后一个完成的任务的名字，否则获取[ITBEG]对应的字符，即`In the beginning`
// 从已经加载到内存的GXT内容中搜索任务名
lastMissionPassed = TheText.Get(CStats::LastMissionPassedName[0] ? CStats::LastMissionPassedName : "ITBEG");
// 使 suffix '.','.','\0',野值，野值]
// 野值会被写入存档中，但是并无影响
AsciiToUnicode("...'", suffix);
suffix[3] = L'\0';
TextCopy(saveName, lastMissionPassed);
int len = UnicodeStrlen(saveName);
saveName[len] = '\0';

// 如果存档名长度超过22,即超过11个字符
if (len > ARRAY_SIZE(saveName)-2)
    // 将suffix复制到saveName的最后
    TextCopy(&saveName[ARRAY_SIZE(saveName)-ARRAY_SIZE(suffix)], suffix);
// 将最后一个野值替换为 '\0'，但由于suffix[3]已经是'\0'所以这句话没有实质性作用
saveName[ARRAY_SIZE(saveName)-1] = '\0';
```

### 编码

而引起游戏崩溃的关键就在于获取任务名这一步

游戏的字库是通过一张保存了所有字符的大贴图实现的，每个字符都有自己的唯一编号，存档文件中即使用这个编号表示

比如英文原版中，`The Party`在存档中是这样保存的，其实就是ASCII

```
5400 6800 6500 2000 5000 6100 7200 7400 7900
T    h    e         P    a    r    t    y
```

对于英文版来说，使用ASCII码是十分方便的，而对于拉丁语系的其他语言，也仅仅是多加一些符号的问题

但是对于日语和汉语，字符实在太多了，这个时候编号就会变得多起来，但是这个编号**最开始的部分必须和英文版的一样**，不然可能会出现应该显示字母`A`但显示汉字`阿`的情况。

现实的计算机操作系统中的编码也是如此，编码不兼容ASCII就会出现各种想不到的问题。

到这里，就可以解释最初的“单项兼容”问题了

# 问题解析

在使用`TheText.Get(*const wchar)`获取任务名时，是基于当前的游戏的语言设定的。如果使用了汉化补丁，那么获取到的字符编码很大概率对于英文原版来说都是**不认识的**（除非有纯英文，数字的任务名），这个时候游戏又没有做对应的处理（毕竟汉化是第三方作品），那么游戏就炸掉了

反过来，汉化编码是兼容英文的，所以无论如何都是可以正常显示存档名，然后正确读取的

# 如何修复存档

知道原理就很轻松的可以想到，**将存档名中的非ASCII编码去掉就行**	

但需要注意的是，游戏存档中有一个校验和，储存在存档的最后4个字节，使用**小端储存**方式

算法如下：

设置一个`unsigned int`型变量，以单个字节为基本单元，将`[0,201824)`区间的所有字节对应值相加，即为校验和，并从`201824`开始写入

此处给出C#实现：


```csharp
public static void FixCheckSum(string path)
{
    uint checkSum = 0;
    using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
    {
        while (reader.BaseStream.Position < reader.BaseStream.Length && reader.BaseStream.Position < 201824)
        {
            checkSum += reader.ReadByte();
        }
    }
    using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Open)))
    {
        writer.BaseStream.Seek(201824, SeekOrigin.Begin);
        writer.Write(checkSum);
    }
}
```
