
功能：分析奇迹运行状态并邮件通知，邮件中同时附带抓屏图片


1, 用管理员身份运行
因为这个助手是通过抓取网路包来分析奇迹状态，所以需要网络安全包

2, 确保安装了DotNet Framework 4.6， Win10亲测有效

3，配置 MuMonitor.exe.config (一次性)
ProcessName：main, 不用修改，代表奇迹进程名
CheckIntervalInMins：检测间隔时间（分钟),建议不少于5分钟
EmailAddress： 你用来发送和接受状态的邮箱地址
EmailPassword： 邮箱密码, 只会在你本地保存
ShutdownOnDisconnected： True 或者 False, 如果是True，在检测到所有奇迹进程都不活动后，关闭电脑，请慎重设置

注意：
如果机器锁屏了，抓屏图片就会是锁屏界面，所以如果特别想看到正确的奇迹抓屏，建议不要锁屏