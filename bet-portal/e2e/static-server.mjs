import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { extname, resolve, sep } from 'node:path';

const root = resolve('dist/bet-portal/browser');
const indexPath = resolve(root, 'index.html');
const contentTypes = new Map([
  ['.css', 'text/css; charset=utf-8'],
  ['.html', 'text/html; charset=utf-8'],
  ['.ico', 'image/x-icon'],
  ['.js', 'text/javascript; charset=utf-8'],
  ['.json', 'application/json; charset=utf-8'],
  ['.svg', 'image/svg+xml'],
]);

const server = createServer(async (request, response) => {
  if (request.method !== 'GET' && request.method !== 'HEAD') {
    response.writeHead(405, { Allow: 'GET, HEAD' }).end();
    return;
  }

  const pathname = new URL(request.url ?? '/', 'http://localhost').pathname;
  let filePath;

  try {
    filePath = resolve(root, `.${decodeURIComponent(pathname)}`);
  } catch {
    response.writeHead(400).end();
    return;
  }

  if (filePath !== root && !filePath.startsWith(`${root}${sep}`)) {
    response.writeHead(400).end();
    return;
  }

  try {
    const fileStat = await stat(filePath);
    if (!fileStat.isFile()) {
      if (extname(pathname)) {
        response.writeHead(404).end();
        return;
      }
      filePath = indexPath;
    }
  } catch (error) {
    if (error instanceof Error && 'code' in error && error.code === 'ENOENT') {
      if (extname(pathname)) {
        response.writeHead(404).end();
        return;
      }
      filePath = indexPath;
    } else {
      console.error('Failed to inspect the browser build:', error);
      response.writeHead(500).end();
      return;
    }
  }

  try {
    const content = await readFile(filePath);
    response.writeHead(200, {
      'Content-Type': contentTypes.get(extname(filePath)) ?? 'application/octet-stream',
      'Cache-Control': 'no-store',
    });
    response.end(request.method === 'HEAD' ? undefined : content);
  } catch (error) {
    console.error('Failed to serve the browser build:', error);
    response.writeHead(500).end();
  }
});

server.listen(4300, '127.0.0.1', () => {
  console.log('Serving the client-only browser build at http://127.0.0.1:4300');
});
