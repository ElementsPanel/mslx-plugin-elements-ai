// Uses the same public app and OAuth endpoints as MSLX's MSLFrp login page.
const origin = 'https://user.mslmc.net';
const appid = 'tKYvKk48Sq5kGAy12IJQxLEKhXx';

export async function loginMslFrp(signal: AbortSignal): Promise<void> {
  const popup = window.open('about:blank', 'MSLX-MSLFrp-Login', 'popup,width=600,height=600');
  if (!popup) throw new Error('请允许弹出窗口后重新点击登录。');
  popup.opener = null;
  const controller = new AbortController();
  const abort = () => controller.abort();
  signal.addEventListener('abort', abort, { once: true });
  if (signal.aborted) abort();
  const closed = setInterval(() => { if (popup.closed) abort(); }, 500);
  const request = async (path: string, body?: object) => {
    const response = await fetch(origin + path, {
      method: body ? 'POST' : 'GET', credentials: 'omit', cache: 'no-store',
      headers: body ? { 'Content-Type': 'application/json' } : {},
      body: body ? JSON.stringify(body) : undefined, signal: controller.signal,
    });
    if (!response.ok) throw new Error('MSL 登录服务暂时不可用，请重试。');
    return response.json();
  };
  try {
    const csrf = Array.from(crypto.getRandomValues(new Uint8Array(16)), b => b.toString(16).padStart(2, '0')).join('');
    const session = await request('/api/oauth/createAppLogin', { appid, csrf });
    const { ssid, url } = session.data || {};
    if (typeof ssid !== 'string' || !ssid || typeof url !== 'string') throw new Error('无法创建 MSL 登录请求，请重试。');
    const target = new URL(url);
    if (target.origin !== origin) throw new Error('MSL 登录地址无效。');
    controller.signal.throwIfAborted();
    popup.location.href = target.href;
    popup.focus();
    const deadline = Date.now() + 10 * 60 * 1000;
    while (!controller.signal.aborted) {
      if (Date.now() > deadline) throw new Error('MSL 登录等待已超时，请重新点击登录。');
      await new Promise<void>((resolve, reject) => {
        const cancel = () => { clearTimeout(timer); reject(new DOMException('Aborted', 'AbortError')); };
        const timer = setTimeout(() => { controller.signal.removeEventListener('abort', cancel); resolve(); }, 1000);
        controller.signal.addEventListener('abort', cancel, { once: true });
        if (controller.signal.aborted) cancel();
      });
      const result = await request('/api/oauth/appLogin?' + new URLSearchParams({ csrf, ssid }));
      const token = result.data?.token;
      if (typeof token === 'string' && token && token.length <= 8192 && !/[\x00-\x1f\x7f]/.test(token)) {
        controller.signal.throwIfAborted();
        localStorage.setItem('msl-user-token', token);
        return;
      }

    }
  } finally {
    clearInterval(closed);
    signal.removeEventListener('abort', abort);
    controller.abort();
    if (!popup.closed) popup.close();
  }
}
