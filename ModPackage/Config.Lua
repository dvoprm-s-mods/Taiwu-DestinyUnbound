return {
    Title = "天命无拘 / Destiny Unbound",
    Author = "Dvoprm",
    Version = "0.1.0",
    GameVersion = "1.0.46",

    Description = [=[
诊断版：只打印七元万道相关状态，不修改存档。
]=],

    BackendPlugins = {
        [1] = "DestinyUnbound.dll",
    },

    HasArchive = false,
    NeedRestartWhenSettingChanged = true,
}