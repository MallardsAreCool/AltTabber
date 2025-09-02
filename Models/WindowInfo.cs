namespace AltTabber.Models
{
    internal class WindowInfo
    {
        public IntPtr Hwnd { get; set; }
        public required string Title { get; set; }
        public required string ProcessName { get; set; }
        public char HotKey { get; set; }
        public IntPtr Icon { get; set; }
    }
}
