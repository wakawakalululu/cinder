// =====================================================================================
// Long-form help content for every built-in tool.
//
// Each block is a `partial` override of `HelpMarkdown` on the matching tool view-model.
// The text is written for someone with no forensics background: what the artifact is, why
// an investigator cares, and how the tool works.
//
// THE RULE FOR THIS FILE: describe what the tool does today. Every step, column and limit
// named here must exist in the code. A feature that is planned belongs in ROADMAP.md, not
// here. An examiner who reads "first plug-in time" and finds a key last-write time has been
// misled, and help text is the easiest place to do that by accident. When a tool changes,
// change its help in the same commit. Each block ends with "## Limits" where there are any.
//
// Light "## Section" markers are interpreted by the help flyout — a line starting with
// "## " becomes a heading; "- " and "1. " become list items; blank lines break paragraphs.
// =====================================================================================
namespace Cinder.App.ViewModels.Tools;

// ----- EXAMINE -----------------------------------------------------------------------

public sealed partial class HexTool
{
    public override string HelpMarkdown => """
## 这是什么
以字节级查看构成文件的原始字节，就像记事本显示字符那样，只是粒度到了字节。计算机上的每个文件都以字节形式存储，十六进制查看器让你直接查看这些字节，不经任何程序先行解释。

## 何时使用
文件在它的正常程序里打不开时、怀疑文件被改动过时、需要找到解析器漏掉的内容时，或者想通过文件开头几个字节（"魔数"）确认文件类型时。这也是查看可疑文件最安全的方式——不会执行任何代码。

## 在 Cinder 中怎么用
1. 点击 "Open file" 或按 Ctrl+O，然后选择任意文件。每个文件在独立标签页中打开。
2. 字节以十六进制（00–FF）显示在左侧，右侧是对应的文本表示。
3. 点击任意字节放置光标。Inspector 窗格会把该处字节按常见的整数、浮点数和日期类型解码，便于你发现时间戳和结构。Ctrl+Shift+B 可显示或隐藏 Inspector。
4. 按 Ctrl+F 可搜索文本、UTF-16 文本、字节模式（`50 4B 03 04`）或正则表达式。Enter 执行搜索；F3 和 Shift+F3 在命中项之间逐个跳转。
5. 按 Ctrl+G 跳转到某个偏移（0x 开头为十六进制，或十进制）。
6. 点击并拖动可选择一段范围。Ctrl+C 将选中内容以十六进制复制。
7. Ctrl+D 为光标位置添加书签，便于之后跳回。

## 小提示
几乎所有文件格式的开头 2–8 个字节都是唯一的文件签名。文件以 `50 4B 03 04` 开头就是 ZIP（或 Office 文档）；以 `25 50 44 46` 开头就是 PDF，无论扩展名写的是什么。
""";
}

public sealed partial class StringsTool
{
    public override string HelpMarkdown => """
## 这是什么
一个把文件中所有可读文字提取出来的扫描器——用户名、URL、文件路径、错误消息，任何看起来像人写的文本。即使是编译后的程序和磁盘镜像，里面也有成千上万条这样的字符串。

## 何时使用
想在不做完整分析的前提下快速了解一个二进制文件大概是什么时。常见用途：发现恶意程序中硬编码的 URL、从损坏的文档中抢救文本、找出能说明程序用途的线索、提取程序访问过的路径。

## 在 Cinder 中怎么用
1. 点击 "Pick file…" 并选择任意文件——可执行文件、图片、内存转储都可以。
2. 筛选行左侧的选择器决定筛选方式："Contains text" 是普通子串匹配，"Regex" 是 .NET 正则表达式，其余都是预设——电子邮件地址、URL、IP 地址、域名、支付卡号、电话号码、加密货币地址、哈希、Windows 路径等。选定预设后列表只显示包含它的字符串；再在框里输入可进一步收窄（如 "Email addresses" + "example.com"）。
3. Export 菜单会把列表当前显示的内容写出。
4. "Hide gibberish" 会隐藏几乎可以断定是压缩字节巧合产生的短串。想要可读的输出就保持开启；要找极短的标记就关闭它。
5. "Min length" 控制多长的一段可打印字节才计入。默认 6 比较合理；调大可减少噪声，调小可捕捉更短的标记。修改后请用 "Scan again" 重新扫描。
6. 每行显示偏移、编码（ASCII 或 UTF-16）和字符串本身。

## 关于压缩文件的提醒
打开 `.docx`、`.xlsx`、`.zip`、`.gz`、`.pdf` 等文件时，会出现一条横幅说明：显示的字符串只是容器元数据——文档正文位于压缩流内部，无法从原始字节中读出。对 Word 文档，请改用文档解析工具。

## 限制
只能找到 ASCII 和 UTF-16（小端）文本；其他编码的文本找不到。一次扫描在 50,000 条字符串后停止，因此在大型镜像上列表可能只覆盖文件的开头部分。
""";
}

public sealed partial class GalleryTool
{
    public override string HelpMarkdown => """
## 这是什么
一个文件夹图片的缩略图网格。选中某张图片会全尺寸显示，并列出文件内部存储的细节：相机品牌与型号、曝光参数、相机记录的日期，以及（若有）GPS 位置。

## 何时使用
查看手机备份里恢复出的照片文件夹或 "Pictures" 目录中的照片时。取证员通常查找 GPS 坐标（照片在哪里拍的）和内嵌日期（何时拍的）。

## 在 Cinder 中怎么用
1. 点击 "Pick folder…" 并选择一个文件夹。子文件夹也会被搜索。
2. 缩略图边读取边显示，顺序与文件被发现的顺序一致。
3. 点击缩略图可查看完整图片及其大小、像素尺寸、文件修改时间、GPS 位置和相机细节。

## 限制
- 每个文件夹最多显示 500 张图片。更多时状态栏会说明；可改选子文件夹查看其余图片。
- Cinder 无法渲染的格式（HEIC、AVIF）会列出但没有缩略图和预览，其内嵌细节仍会尽量读取。
- "Modified" 是文件在磁盘上的最后修改时间，复制文件可能改变它。相机自身记录的日期在详情列表中。
""";
}

public sealed partial class DocumentsTool
{
    public override string HelpMarkdown => """
## 这是什么
一个把文档中的文字提取出来、以纯文本显示的读取器，不需要启动 Word、Excel 或 PDF 程序。

## 何时使用
在不冒宏被运行风险的情况下阅读附件，或者在创建该文件的程序未安装时读取其中的文字。

## 在 Cinder 中怎么用
1. 点击 "Pick document…" 并选择 PDF、Word、Excel、PowerPoint、OpenDocument、EPUB、富文本或纯文本文件。
2. 文本会显示在工具中。若无法提取，工具会说明原因。

## 限制
仅文本：没有页面版式、没有图片，也不显示作者信息、批注或修订。扫描件（文字是图片）没有可提取的文本。
""";
}

public sealed partial class FilesystemTool
{
    public override string HelpMarkdown => """
## 这是什么
磁盘镜像内文件和文件夹的列表。不用启动嫌疑人的操作系统，就能看到原盘上有什么。Cinder 可读取 NTFS（Windows）、FAT（U 盘、较老的系统）、ext2/3/4（Linux）和 ISO 9660（CD/DVD 镜像）。

## 何时使用
拿到磁盘镜像后通常第一步就是它：列目录、四处看看。

## 在 Cinder 中怎么用
1. 选择磁盘镜像（.dd、.raw、.E01、.aff4、.vhd、.vhdx、.iso）。
2. Cinder 识别文件系统——对整盘镜像则逐个分区遍历——并列出每个文件和文件夹。Kind 列说明每行是什么；文件有大小，每个条目都有时间戳。
3. 在 NTFS 卷上，Cinder 随后读取 $MFT 中已不再使用的记录，把它们追加在 `[deleted]/` 路径下，Deleted 列标为 "deleted"，Inode 列给出其 MFT 记录号。Windows 删除文件只需清除一个标志位；名称、大小和时间戳会留在记录里，直到该记录被复用。在筛选框输入 `deleted` 可只看这些。
4. 筛选框可按任意列收窄。Export 菜单写出的就是当前显示的行。
5. 已知文件筛选：在设置中开启 "Hash files during filesystem enumeration"，并通过哈希样本集工具选定哈希样本集数据库。此后每个文件都会得到一个 SHA-1 和一个 Verdict——判定来自哈希样本集，或为 Unknown。在筛选框输入 `Unknown`，已编目的文件就会消失。超过大小上限的文件会标注为 skipped，而不是留空。
6. 选中一行并选择 "Bookmark" 可将其保存为发现项；报告工具会把书签变成附件（Exhibit）。

## 限制
- 大卷上的列表会在固定条目数处停止。届时会有横幅提示说明，导出也只包含当前显示的行。
- 少数行是说明而非文件，Kind 列显示 "Note"：`[image details]`、`[deleted entries]` 和 `[hashing]` 行承载关于镜像或本次运行的信息。
- 列只在有内容可显示时才出现。文件系统为条目分配了 Cinder 能读取的记录号时才显示 Inode：目前只有已删除的 NTFS 条目才有，因此现存文件的 Inode 单元格为空。Verdict 和 Sha1 只在设置中开启文件哈希后才出现。
- 空单元格表示"未知"或"不适用"（文件夹没有大小），绝不表示零。
- FAT 卷上的时间取自设备的本地时钟，没有时区。按存储原样显示并标注 "(local)"，不是 UTC。
- 已删除条目只提供名称和时间戳，不包括内容。要恢复内容，请用文件雕刻工具处理该镜像。
- Cinder 无法识别其文件系统的镜像会报错。
""";
}

public sealed partial class RegistryTool
{
    public override string HelpMarkdown => """
## 这是什么
Windows 注册表是 Windows 和大多数程序存放设置信息的数据库。它保存在少数几个称为配置单元（hive）的文件里：计算机级的 SYSTEM、SOFTWARE、SAM 和 SECURITY，每个用户的 NTUSER.DAT 和 UsrClass.dat。

## 何时使用
想直接查看某个配置单元时：安装过的程序、服务、最近使用的文件、网络设置。其他几个工具（USB 使用记录、网络历史、Shellbags 外壳轨迹、Amcache 执行缓存、ShimCache 兼容缓存）读取配置单元的特定部分并以更方便的形式呈现；本工具显示全部内容。

## 在 Cinder 中怎么用
1. 选择一个配置单元文件。
2. 每行是一个值：所在的键、值的名称和类型、数据，以及该键的最后写入时间（该键内容最后一次变化的时间）。
3. 用筛选框按行内任意文本查找键或值。

## 限制
- 表格最多显示 25,000 个值、键嵌套最多 8 层。达到任一上限时会有横幅提示说明；大型 SOFTWARE 配置单元总是会被截断。
- 超过 256 个字符的值数据会用 "…" 截断显示。
- 不恢复已删除的键和值。
""";
}

public sealed partial class EventLogTool
{
    public override string HelpMarkdown => """
## 这是什么
Windows 把计算机上发生的事情——登录、服务启动、程序崩溃、安全事件——记录在扩展名为 .evtx 的事件日志文件里，位于 `Windows\System32\winevt\Logs`。

## 何时使用
回答"谁在什么时候登录过"、"装过什么"、"日志是否被清除"。Security.evtx 存放登录和账户变更；System.evtx 存放服务和关机；许多程序也有自己的日志。

## 在 Cinder 中怎么用
1. 选择一个 .evtx 文件。
2. 每行是一个事件：时间（UTC）、通道、提供程序、事件 ID、级别、计算机、用户和简短描述。
3. 在筛选框输入事件 ID 或任意文本可收窄列表。例如 4624 是登录成功，1102 是安全日志被清除。

## 限制
表格最多显示 100,000 个事件并会说明。要把这些事件与其他检材按时间顺序放在一起查看，可在超级时间线工具中导入该文件夹。
""";
}

public sealed partial class PrefetchTool
{
    public override string HelpMarkdown => """
## 这是什么
为了让程序启动更快，Windows 会为运行过的每个程序在 `Windows\Prefetch` 保留一个小文件。副作用是每个 .pf 文件都记录了该程序运行过、运行了多少次、以及何时运行。

## 何时使用
证明某个程序在这台计算机上运行过——即使程序本身后来已被删除。

## 在 Cinder 中怎么用
1. 选择 Prefetch 文件夹，或单个 .pf 文件。
2. 每行是一个 .pf 文件：程序名、运行次数、最后运行时间、最多八个近期运行时间，以及它加载了多少文件和文件夹。

## 限制
- 一个文件夹最多读取 5,000 个文件；更多时会有横幅提示说明。
- 不搜索子文件夹。
- 无法解析的文件仍会留在列表中，原因写在 Note 列。
- Windows 10 和 11 的预取文件使用了特殊的压缩方式，只有 Cinder 运行在 Windows 上时才能读取。
""";
}

public sealed partial class ShellbagsTool
{
    public override string HelpMarkdown => """
## 这是什么
Windows 记得每个文件夹窗口的布局方式。为此它记录了用户在文件资源管理器中打开过的每个文件夹——包括 U 盘和网络共享上的文件夹，以及此后已被删除的文件夹。

## 何时使用
证明用户浏览过某个特定文件夹，或某个文件夹曾经存在。

## 在 Cinder 中怎么用
1. 选择用户的 NTUSER.DAT 或 UsrClass.dat（Windows 7 及以后，大多数 shellbags 在 UsrClass.dat 中）。
2. 每行是一个文件夹：尽可能还原出的路径、其下记录的子文件夹数量，以及其注册表键的最后写入时间。

## 限制
- 键最后写入时间是记录最后一次变化的时间。它通常是、但不总是文件夹最后一次被打开的时间。
- 文件夹名从存储的记录还原；Cinder 无法识别的记录形式会显示其中可读的文本，或键的编号。
- 表格最多显示 25,000 个文件夹并会说明。
""";
}

public sealed partial class JumplistsTool
{
    public override string HelpMarkdown => """
## 这是什么
在 Windows 任务栏上右键一个程序就能看到它最近的文件。这个列表就是跳转列表（jump list），按程序分别存储在用户的 `Recent\AutomaticDestinations` 和 `Recent\CustomDestinations` 文件夹中。

## 何时使用
查明用户用某个特定程序打开过哪些文件和文件夹，包括已拔除的可移动驱动器上的文件。

## 在 Cinder 中怎么用
1. 选择上述文件夹之一，或单个跳转列表文件。
2. 每行是一个条目：列表属于哪个程序、被打开的路径、随条目存储的计算机名和网卡地址、条目的创建和最后修改时间，以及是否被固定。

## 限制
- 最多显示 20,000 个条目；更多时会有横幅提示说明。
- 无法解析的文件仍会留在列表中，原因写在 Note 列。
- 程序以 ID 标识；只有 Cinder 认识的 ID 才显示名称。
""";
}

public sealed partial class LnkTool
{
    public override string HelpMarkdown => """
## 这是什么
`.lnk` 文件是 Windows 快捷方式。每个桌面图标和每个"最近文件"条目都是。快捷方式存储的不只是它指向的路径：还有创建快捷方式时目标文件的时间戳、驱动器的卷序列号，以及通常还有的计算机名和网卡地址。

## 何时使用
把文件与某个特定 U 盘关联（通过卷序列号）、证明文件是从网络共享打开的，或找回已删除文件的路径。

## 在 Cinder 中怎么用
1. 选择一个 .lnk 文件或存放它们的文件夹。子文件夹会被搜索；用户的 Recent 文件夹通常是起点。
2. 每行是一个快捷方式：目标路径、参数、工作目录、快捷方式中记录的目标文件创建/修改/访问时间、卷序列号，以及（如果快捷方式里带有）计算机名和网卡（MAC）地址。

## 限制
- 最多读取 10,000 个快捷方式；更多时会有横幅提示说明。
- 不是每个快捷方式都记录计算机名或 MAC 地址；没有时相应单元格为空。
- 无法解析的文件仍会留在列表中，原因写在 Note 列。
""";
}

public sealed partial class BrowserHistoryTool
{
    public override string HelpMarkdown => """
## 这是什么
浏览器把用户访问过的地址存在数据库里。Chrome、Edge、Brave 和 Opera 存在名为 History 的文件中；Firefox 使用 places.sqlite。

## 何时使用
查看用户看过、搜过什么，以及最后访问的时间。

## 在 Cinder 中怎么用
1. 选择浏览器配置文件夹——或包含多个配置的上一级文件夹——或单个 History 或 places.sqlite 文件。
2. 每行是一个地址：浏览器、来自哪个数据库（便于区分两个配置）、最后访问时间（UTC）、地址、页面标题和访问次数。

## 限制
- 每个地址一行，显示其最近一次访问。更早的各次访问不逐一列出。
- 单个数据库最多读取 50,000 个地址，总量上限 100,000，按最新优先。达到任一上限时会有横幅提示说明。
- Cinder 读取每个数据库的私有副本（存在预写日志时也会一并读取），从不写原始文件。
- 浏览器名称根据文件夹路径推断。复制到异常位置的配置会标注为无法识别的 Chromium 内核浏览器。
- 下载记录、cookie、保存的登录信息和缓存页面不显示。
""";
}

public sealed partial class UsbHistoryTool
{
    public override string HelpMarkdown => """
## 这是什么
Windows 记录每一台连接过的 USB 存储设备：厂商、型号，以及（对多数设备而言）序列号。记录存放在 SYSTEM 注册表配置单元中。

## 何时使用
证明某个特定的 U 盘或驱动器连接过这台特定的计算机。

## 在 Cinder 中怎么用
1. 选择 SYSTEM 配置单元（`Windows\System32\config\SYSTEM`）。
2. 每行是某个控制集里的一台设备：设备类别（厂商和型号）、实例名（通常就是序列号）、友好名称和描述，以及该设备注册表键的最后写入时间。

## 限制
- 键最后写入时间是 Windows 最后一次修改该键的时间。它不是首次或最后一次连接的时间，而且较新版本的 Windows 会在系统更新期间重写这些键。连接时间需要其他来源，例如安装日志和事件日志，本工具不读取这些。
- 不显示盘符，也不显示连接设备的用户。
- 实例名第二个字符为 "&" 的，是 Windows 在设备没有自身序列号时生成的；它不能对应某一台具体设备。
- 没有控制集的配置单元会报 "not a SYSTEM hive"。有控制集但没有 USB 存储键的 SYSTEM 配置单元会报告为未记录任何设备。
""";
}

public sealed partial class WifiHistoryTool
{
    public override string HelpMarkdown => """
## 这是什么
Windows 在 SOFTWARE 注册表配置单元中，为计算机加入过的每个网络（无线或有线）保留一个配置文件。每个配置文件包含网络名称，以及计算机首次和最近一次连接它的日期。

## 何时使用
证明计算机连接过某个特定网络——家庭网络、酒店、公司访客网络——以及连接时间。

## 在 Cinder 中怎么用
1. 选择 SOFTWARE 配置单元（`Windows\System32\config\SOFTWARE`）。
2. 每行是一个网络配置文件：名称和描述、属于无线/有线/移动宽带中的哪一种、Windows 给它的类别（Public、Private 或 Domain）、首次和最后连接时间，以及该配置文件注册表键的最后写入时间。

## 限制
- 首次和最后连接时间以计算机本地时间存储，没有时区。按存储原样显示；与 UTC 时间对比前，先从 SYSTEM 配置单元推算时区。
- 不显示接入点的硬件地址（BSSID）和安全类型。
- 不是 SOFTWARE 配置单元的文件会报错。
""";
}

public sealed partial class SrumTool
{
    public override string HelpMarkdown => """
## 这是什么
SRUM（System Resource Usage Monitor，系统资源使用监视器）是 Windows 的一个数据库，大约每小时记录一次每个程序占用了多少处理器工作量、磁盘和网络流量，以及在哪个用户账户下运行。它位于 `Windows\System32\sru\SRUDB.dat`，保留约一个月的历史。

## 何时使用
证明某个程序在某天运行过或传输过数据，即使程序已经不在——例如文件传输工具发送了多少字节。

## 在 Cinder 中怎么用
1. 选择 SRUDB.dat。
2. 每行来自三张表之一，表名在 Table 列中："app"（前台和后台的处理器周期数，以及读写的字节数）、"net"（发送和接收的字节数）或 "energy"（电池容量读数）。行中给出时间（UTC）、用户、程序和数值。处理器周期是工作量的计数，不是时长。

## 限制
- 每张表最多读取 25,000 行。表更大时会有横幅提示说明。
- 读取该数据库需要用到 Windows 组件，因此本工具只能在 Windows 上运行。
- 从正在运行的计算机上复制的数据库可能被标记为未正常关闭而拒绝打开。错误信息会说明；用 `esentutl /p` 修复副本后，打开修复后的副本。
- SRUM 的其他表（网络连接、推送通知）不读取。
""";
}

public sealed partial class AmcacheTool
{
    public override string HelpMarkdown => """
## 这是什么
Amcache 是一个注册表配置单元，Windows 在其中记录它见过的程序文件，包括每个文件的路径、SHA-1 哈希、发布者和版本。它位于 `Windows\AppCompat\Programs\Amcache.hve`。

## 何时使用
确认某个程序文件曾存在于计算机上，并通过哈希精确识别是哪个文件——即使文件已被删除。

## 在 Cinder 中怎么用
1. 选择 Amcache.hve。
2. 每行是一个文件：名称、路径、SHA-1、发布者、程序 ID、版本、大小及其注册表键的最后写入时间。
3. 把 SHA-1 复制到哈希样本集或 VirusTotal 查询工具，看它是否已知。

## 限制
- 条目只说明文件曾存在并被 Windows 注意到。仅凭它不能证明程序运行过，工具也不显示执行时间。
- 键最后写入时间是条目最后一次更新的时间，不是文件首次出现的时间。
- Windows 只对大文件的前 31 MB 计算哈希，因此更大文件的 SHA-1 不会与整个文件的哈希一致。
- 最多显示 25,000 个条目；更多时会有横幅提示说明。
""";
}

public sealed partial class ShimcacheTool
{
    public override string HelpMarkdown => """
## 这是什么
ShimCache（应用兼容性缓存）是 Windows 维护的一份列表，记录它检查过、用于判断是否需要兼容性修复的程序文件。它存储在 SYSTEM 注册表配置单元中。

## 何时使用
证明某个程序文件在某个路径下存在，包括后来被删除的文件和已拆下的驱动器上的文件。

## 在 Cinder 中怎么用
1. 选择 SYSTEM 配置单元（`Windows\System32\config\SYSTEM`）。
2. 每行是一个条目：来自哪个控制集、在缓存中的位置（0 是最近加入的）、文件路径，以及 Windows 记录的该文件自身的最后修改时间。

## 限制
- 显示的时间是文件自身的最后修改时间，不是它被运行或加入缓存的时间。在 Windows 10 和 11 上，条目不能证明程序运行过。
- 缓存在 Windows 关机时写入配置单元，因此从正在运行的计算机取得的配置单元缺少自上次关机以来的活动。
- 支持 Windows 8、8.1、10 和 11。更旧版本 Windows 的缓存会报告为无法解码。
""";
}

public sealed partial class RecycleBinTool
{
    public override string HelpMarkdown => """
## 这是什么
文件被送进回收站时，Windows 会把它移入隐藏的 `$Recycle.Bin` 文件夹，并写一个以 `$I` 开头的小伴随文件。该文件记录条目来自哪里、多大、何时删除。内容本身放在旁边，同名但以 `$R` 开头。

## 何时使用
查明用户删除了什么、从哪里删除、何时删除——包括内容已被清空但 `$I` 记录已被恢复的情况。

## 在 Cinder 中怎么用
1. 选择 `$Recycle.Bin` 文件夹、其中某个用户的文件夹，或单个 `$I` 文件。
2. 每行是一个被删除的条目：所有者（取自文件夹名的用户安全标识符）、原始路径、原始大小、删除时间（UTC），以及旁边是否存在对应的 `$R` 内容。

## 限制
- 所有者以安全标识符显示，不是用户名。
- 本工具列出删除记录。它不打开或还原被删除的内容。
- 无法读取的记录仍会留在列表中，原因写在 Note 列。
""";
}

public sealed partial class EmailTool
{
    public override string HelpMarkdown => """
## 这是什么
列出邮件文件中的邮件：每封的发件人、收件人、主题和时间。它能读取单封邮件（Outlook 的 .msg、多数其他程序的 .eml）、MBOX 邮箱，以及——在安装可选附加组件后——Outlook 的 .pst 和 .ost 邮箱。

## 何时使用
概览一个邮箱或一封保存的邮件：谁在给谁写信、关于什么、什么时候。

## 在 Cinder 中怎么用
1. 选择 .msg、.eml、.mbox、.pst 或 .ost 文件。
2. 每行是一封邮件：发件人、收件人（To 和 Cc）、主题、发送时间；单封邮件还显示文件大小和附件数。
3. 可按任意文本筛选、导出列表，或为某行添加书签。

## 限制
- 仅邮件头：不显示邮件正文，也不能从这里打开或保存附件。
- .pst 和 .ost 文件需要已安装 libpff-python 包的 Python。缺少时工具会说明需要安装什么。这两类邮箱的列表显示文件夹、发件人名称、主题、投递时间和附件数；不读取收件人。
- MBOX 文件的发送时间是邮件 Date 头的原样值（含其时区）；不转换为 UTC。
- 最多显示 50,000 封邮件；更多时会有横幅提示说明。
""";
}

public sealed partial class LinuxArtifactsTool
{
    public override string HelpMarkdown => """
## 这是什么
读取 Linux 系统为用户和活动保留的纯文本文件：登录记录、系统和认证日志、每个用户的 shell 命令历史，以及账户文件。

## 何时使用
调查 Linux 服务器或工作站：谁从哪里登录、运行过什么命令、存在哪些账户。

## 在 Cinder 中怎么用
1. 选择已挂载 Linux 文件系统或应急分诊采集包的顶层文件夹——即包含 `var`、`etc` 和 `home` 的那个文件夹——或选择单个日志文件。
2. 从文件夹读取时，下列文件若存在则会被读取：`var/log/auth.log` 或 `secure`、`var/log/syslog` 或 `messages`、`var/log/wtmp` 和 `btmp`（登录和失败登录）、`etc/crontab`、`etc/passwd`、`etc/shadow`、`etc/ssh/ssh_known_hosts`，以及 `home` 下每个用户和 root 的 bash、zsh、fish 和 Python 历史。
3. 每行是一行日志或一条记录：来自哪个文件、类别、时间、该时间的含义、（可知时的）用户，以及文本。

## 时间列的解读
- "UTC" —— 记录自带时区或本就以 UTC 存储，已做换算。
- "log's local time" —— 有完整日期时间但没有时区：写日志的那台计算机的时钟。
- "no year in log" —— 较旧的日志格式只写月、日和时间。Cinder 按 `MM-DD HH:MM:SS` 显示，不猜测年份。
- shell 历史、账户文件和计划任务文件每行没有时间。

## 限制
- 所有文件显示在一个列表里；用筛选框（例如输入类别名）查看某一类。
- 不读取 systemd journal、轮转或压缩的日志、每用户的计划任务、包管理器日志和回收站文件夹。
- 最多显示 50,000 行；更多时会有横幅提示说明。
""";
}

public sealed partial class MemoryTool
{
    public override string HelpMarkdown => """
## 这是什么
Windows 计算机内存被捕获那一刻正在运行的程序清单，以树状排列：每个进程列在启动它的进程之下。Cinder 通过运行 Volatility 3——一个独立的开源内存分析程序——来获得这份清单。

## 何时使用
寻找不该存在的进程，或由异常父进程启动的普通进程——例如由文档查看器启动的命令提示符。

## 在 Cinder 中怎么用
1. 为 Cinder 使用的 Python 安装 Volatility 3：`python -m pip install volatility3`。
2. 选择 Windows 内存镜像（.raw、.dmp、.vmem）。
3. 每行是一个进程：ID、父进程 ID、在树中的深度（名称按此缩进）、线程数和句柄数、启动和退出时间，以及 Volatility 能报告时的路径和命令行。

## 限制
- 只有进程树可用。网络连接、已加载模块、注入代码和凭据提取不在此提供；需要时直接运行 Volatility。
- 仅支持 Windows 内存镜像。
- 对一个镜像的首次运行可能需要几分钟，Volatility 可能需要联网下载符号文件。用 "停止" 可结束一次运行。
- Volatility 失败时会显示它自己的错误信息。
""";
}

public sealed partial class NetworkTool
{
    public override string HelpMarkdown => """
## 这是什么
数据包捕获文件（.pcap 或 .pcapng）是对流经网络连接的数据的记录。本工具列出其中的数据包。

## 何时使用
查看计算机与哪些地址通信、用了哪些端口、什么时候。

## 在 Cinder 中怎么用
1. 选择 .pcap 或 .pcapng 文件。
2. 每行是一个数据包：文件中的序号、时间（UTC）、协议、源和目标地址与端口、大小，TCP 还有置位的标志。
3. 按地址或端口筛选即可跟踪某一次会话。

## 限制
- 这只是数据包列表。不重组会话，也不提取 Web 请求、域名查询和传输的文件。
- 最多显示 50,000 个数据包；捕获文件更大时会有横幅提示说明。
- 如果文件被截断或中途损坏，会显示损坏之前的数据包，并用横幅说明读取了多少、其余缺失。

## 需要安装什么
读取捕获文件使用的库与录制实时流量的库相同，它不属于 Cinder。Windows 上请安装 Npcap（npcap.com）；Linux 和 macOS 上请用包管理器安装 libpcap。缺少时工具会说明缺的是哪一个。
""";
}

public sealed partial class MobileTool
{
    public override string HelpMarkdown => """
## 这是什么
列出手机或平板备份中的文件：iPhone 或 iPad 由 Finder 或 iTunes 制作的备份文件夹，或用 adb 制作的 Android 备份文件（.ab）。

## 何时使用
在提取任何东西之前，先弄清备份里有什么——哪些应用有数据，某个数据库或文件在哪里。

## 在 Cinder 中怎么用
1. iPhone 或 iPad：选择备份文件夹，即包含 Manifest.db 的那个。Android：选择 .ab 文件。
2. 每行是备份中的一个文件。iPhone 备份：域（属于哪个应用或系统的哪个部分）、是文件/文件夹/链接、设备上的路径，以及它在备份文件夹内的存储名。Android 备份：应用包名、路径、修改时间和大小。

## 限制
- 这只是文件列表。不解码消息、通话记录和应用数据，也不提取文件。
- 加密备份无法打开；工具会说明。
- 最多列出 50,000 个文件；备份更大时会有横幅提示说明。
""";
}

// ----- ANALYZE -----------------------------------------------------------------------

public sealed partial class TimelineTool
{
    public override string HelpMarkdown => """
## 这是什么
把 Cinder 能从一个检材文件夹里读出的所有带时间戳的事件汇成一份按时间排序的列表：登录和其他事件日志记录、运行过的程序、创建过的快捷方式、网页访问、发出的邮件、删除的文件，以及 NTFS 日志记录。

## 何时使用
通常是第二步，紧跟在列出磁盘镜像之后。时间线把"发生了什么"变成一个靠滚动就能回答的问题。

## 在 Cinder 中怎么用
1. 点击 "Ingest folder…" 并选择一个检材文件夹：应急分诊采集包、已挂载镜像的 Windows 文件夹、导出的用户配置。Cinder 会读取在其中找到的事件日志、Prefetch 文件、快捷方式、NTUSER.DAT 配置单元、浏览器历史、邮件文件、回收站记录，以及提取出的 `$J` / `$LogFile` 文件。可以继续导入更多文件夹；它们的事件会加入同一条时间线。
2. 设置 From 和 To 日期以及筛选条件：用户、来源（来源名的一部分，多个用逗号分隔）、摘要中的文本，或 ATT&CK 技术。
3. 在筛选框里按 Enter 或点刷新按钮，条件才会生效。在此之前表格继续显示上次的筛选结果，并有提示说明筛选已更改。
4. 选中一个事件并选择 "Bookmark" 可将其保存为发现项。

## ATT&CK 标签
事件在读取时打标签，仅限映射站得住脚的情形：Windows Security 事件 ID（4624 登录 → T1078 Valid Accounts，4698 → T1053.005 Scheduled Task，7045 → T1543.003 Windows Service，1102 → T1070.001 Clear Event Logs，等等）、Sysmon（8 → T1055 Process Injection）、PowerShell 脚本块日志（4104 → T1059.001）、Prefetch 与 UserAssist 的执行证据（TA0002 Execution）、回收站删除（T1070.004）。在 ATT&CK 框里输入 `T1053` 这样的前缀，即可看到所有匹配的事件。普通的进程创建事件被刻意不打标签。

## 导出
Export 菜单写出与表格当前筛选匹配的全部事件——是全部，不只是屏幕上显示的行——格式为 Timesketch JSONL、Timesketch CSV，或 mactime 能读取的 Sleuth Kit bodyfile 格式。如果你修改了筛选框但尚未应用，导出仍按已应用的筛选执行，并会说明这一点。每次导出都会记入案件的监管链日志。

## 限制
- 表格显示前 5,000 条匹配事件（最早在前），状态栏说明总共匹配多少条。
- 时间线保存在内存中，不存入案件；重启 Cinder 后需要重新导入各文件夹。
- 同一文件夹导入两次，其事件会被加入两次。"…" 菜单中的 "Clear the timeline" 可以清空重来。
- 读取文件夹期间，刷新和导出会等待；"停止" 会放弃本次读取，时间线保持原样。
""";
}

public sealed partial class MapTool
{
    public override string HelpMarkdown => """
## 这是什么
从检材中提取的位置列表：文件夹中每张照片存储的 GPS 位置，外加你手动输入的点位。

## 何时使用
根据设备拍摄的照片，把设备在某个时间定位到某个地点。

## 在 Cinder 中怎么用
1. 点击 "Add photos from folder…" 并选择文件夹。每张带 GPS 位置的照片都会成为一行。
2. 每行有纬度和经度、一个时间，以及该时间的来源：GPS 定位（UTC）、相机时钟（无时区），或两者都没有时的文件修改时间。
3. "Add a point" 手动添加一个位置。它没有时间。
4. Export 菜单把列表写出为 CSV 或 JSON，或写成可在 Google Earth 等地图程序中打开的 KML。

## 限制
- 本工具只列出位置，不绘制地图。
- 只读取照片的 GPS 数据。来自 Wi-Fi 网络、浏览器或基站的位置不收集。
- 每个文件夹最多加入 25,000 个点，停止时状态栏会说明。
""";
}

public sealed partial class GraphTool
{
    public override string HelpMarkdown => """
## 这是什么
从邮件文件构建的"谁给谁写过信"视图：列出找到的每个地址、各自收发了多少封邮件，以及他们之间的邮件。

## 何时使用
在大量邮件中理清头绪：谁最活跃，谁与某个特定地址有过联系。

## 在 Cinder 中怎么用
1. 点击 "Add mail from folder…" 并选择包含 .eml、.msg 或 .mbox 文件的文件夹。子文件夹会被搜索。
2. 人员列表显示每个地址，联系最多的在前。选中一个即可只看发往或来自它的邮件；"Show everyone" 清除选中。
3. "Add a link" 手动记录一次联系。它没有时间。
4. Export 菜单写出的邮件列表。

## 限制
- 是列表，不是绘制出来的关系图。
- 只读邮件文件：聊天记录和 Outlook .pst 邮箱不读取。
- 每个文件夹最多加入 25,000 条发件人到收件人的链接，停止时状态栏会说明。
""";
}

public sealed partial class SearchTool
{
    public override string HelpMarkdown => """
## 这是什么
对一个文件夹中所有文件的全文搜索。Cinder 把文件读一遍建立索引；之后每次搜索都能立刻返回。

## 何时使用
在整个检材文件夹范围内一次性查找某个名字、邮箱地址、账号或短语。

## 在 Cinder 中怎么用
1. 点击 "Index a folder…"，选择要读取的文件夹，再选一个空文件夹存放索引。文档按其文本建索引；其他文件按其中可读的字符串建索引。
2. 输入要找的内容并按 Enter。用 "引号" 搜索短语，词之间可用 AND / OR。
3. 每个结果显示文件、位置和匹配的文本。之前建立的索引可用 "Open existing index…" 重新打开。

## 限制
- 只搜索你建立过索引的文件夹。其他工具里显示的行——注册表值、事件日志条目、浏览器历史——不在索引中，除非它们来源的文件恰好在某个你建过索引的文件夹里，而且也只是以原始文本形式。
- 超大文件会被跳过，状态栏会报告跳过了多少个。
- 一次搜索只显示有限数量的结果，更多时会有说明。
""";
}

public sealed partial class HashSetsTool
{
    public override string HelpMarkdown => """
## 这是什么
一个已知文件哈希的数据库。最著名的来源是 NSRL——NIST 发布的、属于普通软件的文件清单——它能帮你把计算机上数十万个系统文件放到一边，只看剩下的内容。

## 何时使用
减少文件列表里的噪声，或查证某个哈希是否已知。

## 在 Cinder 中怎么用
1. "New hash set…" 新建一个数据库文件；"Open hash set…" 打开你之前建的。Cinder 会记住这个选择。
2. "Import NSRL list…" 把 NSRL minimal CSV 文件导入其中。
3. 粘贴一个哈希并选择 "Look up"，看它是否在数据库中。
4. 要把数据库用于整个磁盘镜像，请在设置中开启 "Hash files during filesystem enumeration"。此后文件系统工具会给每个文件一个 SHA-1 和一个判定结果，在其筛选框输入 `Unknown` 即可隐藏数据库已知的所有文件。

## 限制
只能导入 NSRL minimal CSV 布局。查询需要精确的哈希。
""";
}

public sealed partial class IocMatchTool
{
    public override string HelpMarkdown => """
## 这是什么
输入一份指标（IOC）清单——文件哈希、IP 地址、域名、URL、邮箱地址、文件名、任意文本——并一次性对整个检材文件夹核对，共四种方式：文件哈希、文件路径、文件内文本（ASCII 和 UTF-16 都查），以及时间线能从该文件夹读出的带时间戳事件。

## 何时使用
事件响应的第一个小时：你拿到一份清单，问题是"这里面有没有东西在这台计算机上"。也用于收尾阶段，确认已清理的计算机上不再有它们。

## 在 Cinder 中怎么用
1. 选择指标清单：一个每行一个指标的文本文件。以 `#` 开头的行、空行和 `type,value` 行都可以接受。Cinder 会判断每行属于什么类型，并说明各类找到多少。
2. 选择要扫描的文件夹：应急分诊采集包、已挂载的镜像、导出目录。
3. 运行扫描。"停止" 可提前结束；被停止的扫描不显示任何匹配，因为它没有查看全部内容。每个匹配是一行：指标、类型、匹配位置（哈希、路径、内容或时间线）、文件，以及偏移或时间。
4. 工具栏下方的行会说明哈希和搜索了多少文件、有多少读不了。读不了的文件可能藏着被漏掉的匹配。文件夹里若有块损坏的 E01 或 AFF4 镜像也是一样：这些区域按零读取、会被计数，并且会点名该镜像让你核实。
5. 筛选、导出，或将某行添加为发现项书签。

## 记录什么内容
在监管链日志中，你扫描的文件夹就是检材，指标清单作为核对它的依据一并记录。被加了书签的匹配归属于该文件夹。

## 限制
- 超过 256 MB 的文件不做哈希，超过 512 MB 的文件不做文本搜索。
- 扫描在 200,000 个文件和 50,000 个匹配处停止，并会说明。
- 哈希指标必须是精确的哈希。
- 更换清单或文件夹会停止正在运行的扫描并清空结果；请重新运行扫描。
""";
}

public sealed partial class YaraTool
{
    public override string HelpMarkdown => """
## 这是什么
YARA 是一种描述文件中模式的语言："这段文字，或这串字节"。本工具用你编写或加载的规则扫描一个文件，并列出匹配位置。

## 何时使用
检查文件中是否含有已知恶意软件的字符串，或任何你能写出来的文本或字节模式。

## 在 Cinder 中怎么用
1. 在编辑器里编写规则，或使用 "Load rules file…"。
2. 点击 "Scan a file…" 并选择文件。
3. 结果会列出每个匹配：规则、匹配的字符串和偏移。另一个列表说明每条规则的执行情况：匹配、未匹配，或被跳过及其原因。

## 限制
- Cinder 支持 YARA 语言的一部分：文本字符串（含 `nocase`、`wide` 和 `ascii`）、不带通配符和跳转的十六进制字符串，以及由这些字符串构成的条件（`any of them`、`all of them`、`2 of them`、`$a and $b`）。正则表达式、模块、文件大小测试和其他条件不支持；需要它们的规则会被跳过，并列入已跳过清单。
- 一次只能扫一个文件。内存和未分配空间只能当作文件来扫。
- 一次扫描在固定数量的匹配后停止并会说明。
""";
}

public sealed partial class VirusTotalTool
{
    public override string HelpMarkdown => """
## 这是什么
把文件的哈希——绝不发送文件本身——发给 VirusTotal 查询，显示有多少款杀毒产品标记过具有该哈希的文件。

## 何时使用
在不上传任何东西的前提下，查明某个可疑文件是否已知。

## 在 Cinder 中怎么用
1. 粘贴你的 VirusTotal API 密钥。它只在 Cinder 运行期间保留。
2. 粘贴 MD5、SHA-1 或 SHA-256 哈希并选择 "Look up on VirusTotal"。
3. 结果显示有多少产品判为恶意、可疑、无害或未检出，以及 VirusTotal 首次见到和最近一次分析它的时间。

## 限制
- 一次查一个哈希。
- "No record" 表示 VirusTotal 从没见过该哈希。这不代表文件是安全的。
- 免费 API 密钥每分钟只允许少数几次查询，每天有总量限制。

## 隐私
离开你计算机的只有哈希。哈希本身能暴露你持有某个特定文件——当该文件对你的案件独一无二时，这一点很重要。
""";
}

public sealed partial class AiCopilotTool
{
    public override string HelpMarkdown => """
## 这是什么
一个连接到你自备语言模型的聊天窗口：通过 Ollama 或 LM Studio 运行在你自己计算机上的模型，或提供 OpenAI 兼容接口的托管服务。

## 何时使用
作为面对陌生材料时的研究助手："事件 4769 是什么意思？"、"这条 PowerShell 命令做了什么？"。把要问的文本粘贴进问题里。

## 在 Cinder 中怎么用
1. 选择模型运行的位置，完整填写它的地址（例如 `http://localhost:11434`）和模型名称。托管服务还需要你的 API 密钥。
2. "Test connection" 检查该地址是否有响应。
3. 输入问题并选择 "Send"。目前的对话内容会随每个问题一起发送，因此可以追问。

## 限制
- Cinder 不会自行把检材发给模型。模型只能看到你输入或粘贴的内容。
- 语言模型可能一本正经地说错。在依赖它告诉你的任何内容之前，先对照检材核实。

## 隐私
模型在你自己的计算机上时，什么都不会离开它。使用托管服务时，你输入或粘贴的一切都会按其条款发送给该服务。
""";
}

// ----- ACQUIRE -----------------------------------------------------------------------

public sealed partial class ImagerTool
{
    public override string HelpMarkdown => """
## 这是什么
把整块物理磁盘或现有镜像复制为单个取证镜像文件的工具，过程中对每个字节计算哈希。完成镜像采集后，所有分析都针对副本进行，原盘保持不动。

## 何时使用
几乎所有磁盘调查的第零步：先采集镜像，绝不在原始介质上分析。

## 在 Cinder 中怎么用
1. 通过硬件写保护设备连接源盘。Cinder 只以只读方式打开源，但如果驱动器没有接写保护就直连，Windows 或其他程序仍可能写入，Cinder 阻止不了。
2. 指定源：设备（Windows 上为 `\\.\PhysicalDrive1`，Linux 上为 `/dev/sdb`——都需要 Administrator / root 权限）、普通文件，或 E01（读取时即时解码，因此也可兼作 E01 → raw 转换）。
3. 选择目标和格式。
   - **Raw**：读取字节的同时计算 MD5 / SHA-1 / SHA-256，在镜像旁写入 `.sha256` 伴随文件（sha256sum 格式，镜像校验工具能读取它）和 `.log.json` 采集日志，并把本次运行记入监管链日志。
   - **E01**：EnCase 6 布局，32 KiB 分块，zlib 压缩，1.5 GB 分卷（`.E01`、`.E02`……），头部含案件信息，容器内含 MD5 + SHA-1，不可读扇区记入其错误区。
   - **AFF4**：AFF4 Standard v1.0 ZIP 卷，zlib 压缩的 32 KiB 分块，MD5 / SHA-1 / SHA-256 和案件信息记录在内。
   - **VHD / VHDX**：Hyper-V 或磁盘管理可以附加的动态虚拟磁盘。它们需要预先知道源的大小，因此无法报告大小的设备只能采集为 raw 或 E01。
4. 读取错误：Cinder 会重试，然后把出错的块逐扇区重读，这样一个坏扇区只损失 512 个零字节而不是一兆字节。每个填零扇区都会计数，其偏移记录在日志中。

5. 停止与失败：运行期间，镜像以输出名加 `.partial` 的名字写入，完成后才改名，因此未完成的文件绝不会占用最终文件名。**停止** 会删除该 .partial 文件。如果源停止响应（磁盘正在损坏或被拔出），采集结束，.partial 文件保留，旁边的日志被标记为不完整：这些是来自垂死磁盘的真实数据，但不是完整镜像，也绝不能当作完整镜像使用。

## 哈希值的含义
这些哈希是对*写入内容*的哈希——如果有坏扇区，那就是填零镜像的哈希，而不是原始介质的哈希。日志会说明替换了多少个扇区；请把这个数字写进你的报告。

E01、VHD 和 VHDX 按整扇区存储。如果源不是整数个扇区（例如单个文件），最后一个扇区会以零填充，哈希覆盖的是按存储形态的镜像（含填充），这样镜像校验等工具才能与之吻合。日志也会单独记录仅源字节的哈希。

如果源本身是块损坏的 E01 或 AFF4，这些区域在新镜像中为零，结果会说明受影响的块数。
""";
}

public sealed partial class VerifyTool
{
    public override string HelpMarkdown => """
## 这是什么
镜像创建之后，本工具会重新读取它并重新计算哈希，确认镜像仍与采集时一致。这是能发现存储损坏和篡改的检查。

## 何时使用
每次分析会话之前，或镜像被复制、移动过，或在慢速存储上放置了一段时间之后。

## 在 Cinder 中怎么用
1. 选择镜像文件。
2. Cinder 读取每个块并与参考哈希比较。E01 的参考哈希是采集工具写进容器内的 MD5 / SHA-1；AFF4 容器是其中记录的 MD5 / SHA-1 / SHA-256；raw 镜像则是旁边的 .sha256 / .sha1 / .md5 伴随文件，或紧挨着它的、指名该镜像的 SHA256SUMS 行。

## 结果解读
有三种截然不同的结果，区别很重要。

- Verified —— 存在参考哈希，且磁盘上的字节能复现它。
- VERIFICATION FAILED —— 存在参考哈希但字节与之不符，或块损坏到无法解码。应把镜像视为不可靠，回到源介质。
- Unverifiable —— 没有任何可比对的东西。这不是通过。它意味着容器没有记录采集哈希，也没找到伴随文件，镜像的完整性完全没有得到确认。

## 为什么记录的哈希还不够
文件系统工具在元数据行里显示 E01 记录的 MD5 和 SHA-1，标注 UNVERIFIED。这些是容器对它自己的声明——由生成该文件的程序写下，被篡改或损坏的镜像也会照常显示。只有本工具会重读介质，把这份声明变成一个经核实的结论。
""";
}

public sealed partial class MountTool
{
    public override string HelpMarkdown => """
## 这是什么
把镜像文件以只读驱动器的形式挂载到本机，让其他软件能够浏览它。

## 何时使用
需要运行一个无法读取取证镜像格式的其他程序时。

## 在 Cinder 中怎么用
1. 点击 "Pick image and mount read-only…" 并选择镜像。
2. 工具会显示镜像挂载到了哪里。

## 限制
- 能挂载哪些镜像格式取决于本机安装了什么：Windows 自己就能附加 VHD、VHDX 和 ISO 文件；其他格式需要额外软件，挂载失败时工具会报告。
- Cinder 无法再卸载镜像。请用操作系统自己的工具分离（Windows 上的磁盘管理或 "Eject"，Linux 上的 `umount`）。
- macOS 不提供挂载功能。
""";
}

public sealed partial class ConvertTool
{
    public override string HelpMarkdown => """
## 这是什么
取证镜像格式之间的转换——raw、E01、AFF4、VHD、VHDX——任意方向。源通过能理解它的读取器读入，边读边计算哈希，并以目标格式写出；写完后还会重新读取输出、与该哈希核对。

## 何时使用
下一个工具只认 raw 镜像而检材是 E01 交到你手上时；或者反过来，一个 raw `.dd` 需要以压缩、自带哈希的 E01 形式传递时。

## 在 Cinder 中怎么用
1. 选择源（E01 选第一个分卷即可——其余分卷会自动跟随）和目标格式。
2. 选择输出路径。旁边会写一个 `.log.json`；raw 输出还会得到一个 `.sha256`。
3. 运行。结果行会报告两项核对：
   - 输出重新读取后是否能对上写入时的哈希。对不上就不要使用该输出。
   - 当源是记录了采集哈希的容器时，源本身是否与该哈希相符。不相符的话，在弄清原因之前，两个文件都不要信任。
   如果源容器没有记录哈希，输出仍是它的忠实解码，但没有任何东西把它与原始介质联系起来。
4. 转换会连同哈希一起记入监管链日志。
""";
}

public sealed partial class ShadowCopyTool
{
    public override string HelpMarkdown => """
## 这是什么
Windows 会定期对驱动器做快照，称为卷影副本（Volume Shadow Copy）。快照里常常还保留着后来被修改或删除的文件。

## 何时使用
先弄清 Cinder 当前所在计算机上存在哪些快照，作为用其他工具检查它们之前的第一步。

## 在 Cinder 中怎么用
点击 "List snapshots"。每行是一个快照，含所属卷和创建时间。

## 限制
- 只列出本机的快照。磁盘镜像内的快照不读取，也不能从这里打开或挂载快照。
- 列出快照可能需要以 Administrator 身份运行 Cinder。
""";
}

public sealed partial class RamCaptureTool
{
    public override string HelpMarkdown => """
## 这是什么
采集正在运行的计算机的内存，可以保住关机即失的内容：运行中的程序、网络连接、内存里的口令。

## 当前状态
Cinder 暂时不能采集内存。本界面不做任何采集。

## 替代做法
用专门的程序采集内存——Windows 上是 WinPmem，Linux 上是 LiME——并从可移动介质运行，确保不向嫌疑驱动器写入任何内容。然后把得到的文件在内存取证工具中打开。
""";
}

public sealed partial class CarverTool
{
    public override string HelpMarkdown => """
## 这是什么
文件雕刻恢复通过识别标记文件开头（许多格式还有结尾）的字节来恢复文件，不依赖文件系统。它能找回目录项已经消失的文件。

## 何时使用
从磁盘镜像、内存转储或任何文件中恢复已删除的图片、文档和压缩包。

## 在 Cinder 中怎么用
1. 选择雕刻来源，以及一个用于保存恢复文件的空文件夹。不要存到检材所在的驱动器上。
2. 点击 "Carve"。每个恢复出的文件都会列出：类型、在源中的位置、大小以及保存时的文件名。

## 限制
- 会搜索所有内置文件类型；无法只选其中一部分。
- 文件在磁盘上分片存储时，只能恢复到第一片的末尾，可能打不开。
- 一次运行在固定数量的恢复文件后停止并会说明。
- 恢复出的文件没有原始的文件名、文件夹和时间戳。
""";
}

public sealed partial class CloudPullTool
{
    public override string HelpMarkdown => """
## 这是什么
一个从 Google Drive、OneDrive 或 Dropbox 账户收集文件的工具的开端，需在账户持有人同意下使用。

## 当前状态
Cinder 暂时不能从云账户下载。本界面只为你选择的提供方生成登录链接，使用设置中填写的 client ID。通过该链接登录不会下载任何东西。

## 替代做法
使用提供方自己的导出功能（Google Takeout、OneDrive 或 Dropbox 的文件夹下载），再用其他工具检查下载的文件。
""";
}

// ----- CASE --------------------------------------------------------------------------

public sealed partial class CasesTool
{
    public override string HelpMarkdown => """
## 这是什么
案件是一个单独的文件（扩展名 .cinder），保存你的书签和监管链日志——对取证过程中所做操作的记录。本工具创建案件文件并重新打开最近的案件。

## 何时使用
取证开始时，以及每次回到这个案件时。

## 在 Cinder 中怎么用
1. "New case…" 在你选择的位置创建案件文件。
2. "Open case…" 打开现有案件文件；最近案件会列出，可一键重开。
3. 打开的案件显示在窗口顶部。可以同时打开多个案件；各工具把活动记录到那里当前选中的案件。

## 限制
- 检材不会复制进案件。案件记录的是检查过哪些文件、发现了什么，所以请把检材保存在日志中路径仍然有效的地方。
- 没有打开的案件时，什么都不记录，也无法为行添加书签。
""";
}

public sealed partial class ReportsTool
{
    public override string HelpMarkdown => """
## 这是什么
一个报告生成器：你撰写各小节，Cinder 把你加了书签的发现作为带编号的附件（Exhibit）加入，并把结果导出为 Markdown、HTML、PDF、Word 或 JSON。

## 何时使用
取证收尾时，生成交付的那份文档。

## 在 Cinder 中怎么用
1. 选择模板。它设定小节标题；小节可随意增删。更换模板会保留你已经写好的文字。
2. 逐节撰写。纯文本即可；也支持 Markdown。
3. 勾选 "Include in the report" 后，当前打开案件的书签会成为 Exhibits 小节：每个书签一条带编号的附件，含你的备注、该行的每一列、谁在何时添加的书签。书签在你打开本工具时、切换案件时、以及每次导出前一刻，都会从当前案件重新读取。
4. "Update preview" 以 Markdown 显示报告。Export 写出文件。

## 关于 PDF
PDF 选项写出的是 PDF/A-2b——PDF 的归档形态：字体被嵌入，文件不依赖自身之外的任何内容，因此无论在哪里、什么时候打开，显示效果都应当一致。如果无法生成 PDF/A-2b 文件，导出会失败并说明原因。Cinder 不会退而生成一份普通 PDF 来代替。

## 限制
- 监管链日志不包含在报告中。每次导出会记入日志；请在监管链工具中查看和核对日志。
- 你撰写的文字不保存进案件。关闭 Cinder 前请先导出。
- 报告是否达到法庭或委托人的要求由你判断，不是 Cinder：签字前请通读导出的文档。
""";
}

public sealed partial class CustodyTool
{
    public override string HelpMarkdown => """
## 这是什么
监管链日志是案件的活动记录。每条记录都会计算哈希并与前一条链接，因此被编辑、删除、重排或插入的记录都会破坏链条，校验会指出这一点。

## 记录哪些内容
- 创建案件和打开案件（含计算机名和 Cinder 版本）。
- 每次解析器运行：哪个工具、哪个检材文件、返回了多少行、表格是否因达到上限而截断。
- 每次镜像校验、镜像采集和格式转换，含哈希和判定结果。
- 每次挂载、每次数据导出（哪个工具、格式、路径、行数）和每次报告导出（标题、模板、格式、小节）。
- 添加或删除的书签，以及哈希对话框中的手动哈希。

没有打开案件时什么都不记录。

## 在 Cinder 中怎么用
1. "Check the open case"（或 "Check a case file…"）会重算链条并列出每条记录。
2. 在关键节点 "Sign the log"：一次会话结束时、移交案件前、撰写报告前。Cinder 会用一把 ECDSA P-256 密钥对最新一条记录——其序号、哈希和时间——签名；该密钥在首次使用时创建，存放在你的用户配置目录中。签名连同公钥一起存进案件文件。
3. 可选但推荐：在设置中配置时间戳服务（Time-Stamp Authority）。签名时会同时把签名的 SHA-256 发送给该 RFC 3161 服务，并把它的会签令牌存入案件文件。你的密钥证明"是谁"，时间戳机构证明"不晚于何时"——用的是一台你控制不了的时钟。
4. "Export signature…" 把签名写到文件。把它送到一个你事后无法改动的地方，例如发给主管的邮件或案件工单。

## 它能证明什么
仅靠链条能发现意外损坏和简单的篡改。它挡不住蓄意的重写：哈希没有加密钥，而且与记录存在同一个文件里，任何能写案件文件的人都可以重算整条链，校验照样显示"完好"。

签名改变了这一点。重写之后，被签名位置上的记录不再带有被签的哈希，而重新签名需要私钥——所以任何没有你密钥的人，都无法在不使签名失效的情况下改动该位置之前的日志。剩下的缺口是你自己——密钥的持有者；这正是要把导出的签名送到你自己掌控范围之外的原因。完整论证见 SECURITY.md。
""";
}

public sealed partial class WorkflowsTool
{
    public override string HelpMarkdown => """
## 这是什么
工作流是一份保存好的步骤清单，Cinder 按顺序执行，使同一套流程在每个案件上都能重复。工作流是 JSON 文件。

## 何时使用
每次都以同样方式完成的工作——给镜像算哈希、列出文件、雕刻恢复、写摘要——并且希望能够证明这些工作是以同样的方式完成的。

## 在 Cinder 中怎么用
1. "Open workflow…" 加载工作流文件，并按将执行的顺序列出步骤。
2. "Run" 执行它们。每个完成的步骤都会添加一行，说明它做了什么。

## 可用步骤
- open-image —— 检查文件存在，并将其传给后续步骤。
- hash —— 计算文件的 SHA-256。
- registry —— 打开注册表配置单元并统计其根下的键数。
- fs-enumerate —— 统计磁盘镜像中的文件和文件夹数量。
- carve —— 按文件签名把文件恢复到文件夹。
- report —— 写出一个简短的 Markdown 文件，列出每个步骤的返回结果。
- index —— 把每个步骤的结果加入搜索索引。
- ai-summary —— 尚未提供；会报告为已跳过。

## 限制
- 没有编辑器：工作流文件要手写。
- 某个步骤无法完成工作时，运行在该步停止，消息会点名该步骤和原因。后续步骤不执行。
- 结果不写入案件的监管链日志。
""";
}

public sealed partial class PluginsTool
{
    public override string HelpMarkdown => """
## 这是什么
插件加载器：针对 Cinder 插件接口编写的程序文件（.dll）。

## 何时使用
有人为 Cinder 读不了的格式写了插件时。

## 在 Cinder 中怎么用
1. 点击 "Load plugins from folder…" 并选择存放它们的文件夹。
2. 每个文件都会列出：SHA-256、签名者，以及它是被加载、因不受信任被拒绝还是失败，及原因。

## 限制
- 插件以与 Cinder 本身相同的权限访问这台计算机。只加载你信任的人提供的插件。
- 本版本中，加载插件不会在工具栏上新增工具。
- 不加载 Python 脚本。
""";
}

public sealed partial class SettingsTool
{
    public override string HelpMarkdown => """
## 这是什么
Cinder 偏好设置的存放处：主题、文件系统工具使用的哈希样本集数据库和文件哈希选项、签署监管链日志时使用的时间戳服务，以及 AI 助手使用哪个语言模型。

## 何时使用
首次使用时设置一次，之后每当有 Cinder 需要知道的新东西时再来。

## 需要知道
Ctrl+, 可从任何地方打开设置。在你选择 Save settings 之前什么都不会改变；Discard changes 会把各字段恢复为已保存的值。在 Windows 上，这里保存的 API 密钥会按你的用户账户加密存储。
""";
}
