
功能：分析奇迹运行状态并邮件通知，邮件中同时附带抓屏图片


1, 用管理员身份运行 （右击->以管理员身份运行）
因为这个助手是通过抓取网路包来分析奇迹状态，所以需要管理员权限获取网络流量。

2, Win10亲测有效, Win7/Win8没有测试，如果有问题，请确保安装了 .Net Framework 4.6. 

3，MuMonitor.UI.exe 提供UI支持，使用这个最简单。

如果习惯使用命令行，你可以使用MuMonitor.exe， 但是需要先配置 MuMonitor.exe.config文件(一次性)
	ProcessName：main, 不用修改，代表奇迹进程名
	CheckIntervalInMins：检测间隔时间（分钟),建议不少于5分钟
	EmailAddress： 你用来发送和接受状态的邮箱地址
	EmailPassword： 邮箱密码, 只会在你本地保存
	ShutdownOnDisconnected： True 或者 False, 如果是True，在检测到所有奇迹进程都不活动后，关闭电脑，请慎重设置

注意：
如果游戏窗口最小化了，抓屏图片就会不完整。