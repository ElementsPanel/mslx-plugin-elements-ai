# Elements AI for MSLX

将 `epanel-plugin-elements-ai` 的核心能力迁移到 MSLX 插件体系：通过 OpenAI 兼容接口与 AI 助手对话，并在当前账号权限范围内管理 MSLX 实例。

## 已迁移能力

- OpenAI 兼容的流式对话、推理文本、失败重试与工具调用
- 管理员预设模型和按账号隔离的个人模型
- 按账号隔离的对话历史、批量删除与继续对话
- 默认确认 / 完整操作两种敏感操作模式
- 必要问题的交互式选项或自定义回答
- 实例查询、启动、停止、重启、强制终止、命令发送和安全配置更新
- 实例终端日志读取
- 管理员查询节点及直接执行本机/远程节点系统命令，显示退出码和标准输出/错误
- 实例目录内受限的文件列表、读取、哈希校验编辑、创建、删除与差异预览
- MSLX 内置 Modrinth / CurseForge 资源检索、版本查询和模组/插件下载
- 官方 MSL 服务端源核心列表、版本查询，以及通过 MSL 在线下载核心和 Java 创建实例
- 输入框上方显示下载与安装进度，支持通过 `wait_for_task` 等待后台任务完成
- 全局页眉右侧按钮，以及覆盖页面内容的 AI 侧边栏

## 安全约束

- 所有后端接口要求 MSLX 登录鉴权，权限每次操作都会重新读取宿主账号数据。
- 普通用户只能访问宿主授予的实例；管理员操作不会由“完整操作”模式越权授予普通用户。
- 节点系统命令仅限管理员，默认模式需确认；以目标节点 MSLX 进程的系统账号执行，Docker 部署时运行在容器内。
- 普通用户的个人模型接口禁止访问本机、内网、链路本地地址。
- 文件工具只接受实例目录内的相对路径，拒绝目录穿越、符号链接、二进制/超大文件和无读取哈希的覆盖写入。
- API Key 不会从读取接口返回，也不会写入聊天或工具回执。
- 新建实例不会自动启动；资源下载不会自动安装依赖、重启或热加载实例。

## 开发与构建

前端：

```bash
cd Frontend
pnpm install
pnpm build
```

后端：

```bash
dotnet build MSLX.Plugin.ElementsAI.csproj
dotnet build MSLX.Plugin.ElementsAI.csproj -c Release
```

Release 构建会将依赖合并为插件 DLL，并嵌入 `Frontend/dist` 产物。

回归测试（节点命令、任务等待和账号隔离使用本地进程或模拟响应；前端检查进度轮询与显示）：

```bash
dotnet run --project Tests/NodeCommands/NodeCommands.csproj
node Tests/Frontend/taskProgress.cjs
node Tests/Frontend/modelDraft.cjs
```

## 使用

1. 管理员在 Elements AI 设置中添加至少一个预设模型，或用户添加个人模型。
2. 点击任意页面页眉右侧的 AI 对话按钮打开覆盖式侧边栏；在实例控制台中会自动关联当前实例。
3. 默认模式会在文件写入、命令发送、实例配置变更、资源下载等敏感操作前展示精确参数并等待确认。

管理员可让 AI 先查询 MSL 核心和版本，再使用 `coreSource=msl` 创建实例。创建参数中的 `javaVersion` 支持 `8`、`11`、`17`、`21`、`25`，MSLX 会在后台下载对应 Java 和服务端核心；MSLX 运行在 Docker 中时可省略 `basePath` 使用默认实例目录，创建任务不会自动启动实例。

下载模组/插件或在线创建实例时，输入框上方会显示任务名称、阶段和进度；AI 回复结束后仍会更新后台安装状态。工具回执包含 `taskId`，AI 可调用“等待任务完成”（`wait_for_task`），传入该 ID 和可选的 `timeoutSeconds`（默认 60 秒，范围 1–300 秒）。等待超时会返回尚未完成，停止等待不会取消后台安装。任务进度只对创建任务的账号可见，历史对话会恢复尚未完成任务的查询；宿主已清理的任务会显示状态暂不可用。

管理员也可以要求“在当前节点执行 `uname -a`”。AI 会先通过 `list_nodes` 查询节点，再调用 `execute_node_command`，参数为 `nodeId`、`command`，可选 `workingDirectory`（节点上的绝对路径，默认为 MSLX 程序目录）和 `timeoutSeconds`（默认 30 秒，范围 1–120 秒）。`local` 表示主机节点；远程节点必须已登记在 MSLX 中，并安装、启用新版 Elements AI 插件。命令使用 Linux/macOS 的 `/bin/sh` 或 Windows 的 `cmd.exe`，不支持交互输入和后台任务。超时或停止请求时终止仍在运行的进程树，标准输出和错误各最多保留 16000 个字符。连接中断时执行状态可能未知，应核实结果后再操作。

MSLX 插件开发文档：<https://mslx.mslmc.cn/llms.txt>
