"""Windows Ctrl+wheel -> VRChat OSC. Requires: py -m pip install pynput==1.8.1"""
import atexit
import ctypes
from ctypes import wintypes
import socket
import struct
import sys


def osc_packet(name, value):
    def string(text):
        data = text.encode('utf-8') + b'\0'
        return data + b'\0' * (-len(data) % 4)
    if isinstance(value, bool):
        return string('/avatar/parameters/' + name) + string(',T' if value else ',F')
    return string('/avatar/parameters/' + name) + string(',f') + struct.pack('>f', value)


def main():
    if sys.platform != 'win32':
        raise SystemExit('This helper requires Windows.')
    from pynput import mouse, keyboard
    user = ctypes.WinDLL('user32', use_last_error=True)
    kernel = ctypes.WinDLL('kernel32', use_last_error=True)
    user.GetForegroundWindow.restype = wintypes.HWND
    user.GetWindowThreadProcessId.argtypes = [wintypes.HWND, ctypes.POINTER(wintypes.DWORD)]
    user.GetAsyncKeyState.argtypes = [ctypes.c_int]
    user.GetAsyncKeyState.restype = ctypes.c_short
    kernel.OpenProcess.argtypes = [wintypes.DWORD, wintypes.BOOL, wintypes.DWORD]
    kernel.OpenProcess.restype = wintypes.HANDLE
    kernel.QueryFullProcessImageNameW.argtypes = [wintypes.HANDLE, wintypes.DWORD, wintypes.LPWSTR, ctypes.POINTER(wintypes.DWORD)]
    kernel.CloseHandle.argtypes = [wintypes.HANDLE]
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    amount = 0.0

    def send(name, value):
        sock.sendto(osc_packet(name, value), ('127.0.0.1', 9000))

    def disable():
        send('Kiroshi/Enabled', False)

    def focused():
        pid = wintypes.DWORD()
        user.GetWindowThreadProcessId(user.GetForegroundWindow(), ctypes.byref(pid))
        handle = kernel.OpenProcess(0x1000, False, pid.value)
        if not handle:
            return False
        try:
            path = ctypes.create_unicode_buffer(32768)
            size = wintypes.DWORD(len(path))
            return bool(kernel.QueryFullProcessImageNameW(handle, 0, path, ctypes.byref(size))) and path.value.rsplit('\\', 1)[-1].lower() == 'vrchat.exe'
        finally:
            kernel.CloseHandle(handle)

    def scroll(x, y, dx, dy):
        nonlocal amount
        if not dy or not focused() or not user.GetAsyncKeyState(0x11) & 0x8000:
            return
        amount = max(0.0, min(1.0, round(amount + dy * 0.05, 4)))
        send('Kiroshi/Amount', amount)
        send('Kiroshi/Enabled', amount > 0)
        print(f'Zoom {amount:.0%}', flush=True)

    def keypress(key):
        nonlocal amount
        if key == keyboard.Key.f8 and focused():
            amount = 0.0
            disable()
            print('Optics OFF', flush=True)

    atexit.register(disable)
    print('Enable OSC in VRChat. Ctrl+wheel: zoom; F8: off. Keep this window open. Ctrl+C here exits.')
    # Observe only: do not suppress ordinary mouse/keyboard events system-wide.
    with mouse.Listener(on_scroll=scroll) as ml, keyboard.Listener(on_press=keypress) as kl:
        try:
            ml.join()
        except KeyboardInterrupt:
            ml.stop()
            kl.stop()


if __name__ == '__main__':
    main()
