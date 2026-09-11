"""Session-only, loopback Blender bridge. Run once in Blender's Python console."""
import bpy
import socket
import threading
import queue
import json
import traceback
import io
import contextlib

if not bpy.app.driver_namespace.get('prison_artist_bridge'):
    jobs = queue.Queue()
    namespace = {'bpy': bpy}
    server = socket.socket()
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind(('127.0.0.1', 9877))
    server.listen(4)

    def serve():
        while True:
            conn, address = server.accept()
            raw = bytearray()
            while not raw.endswith(b'\n'):
                part = conn.recv(65536)
                if not part:
                    break
                raw.extend(part)
            reply = queue.Queue()
            jobs.put((json.loads(raw), reply))
            response = reply.get()
            conn.sendall(json.dumps(response).encode('utf-8') + b'\n')
            conn.close()

    def execute_pending():
        try:
            request, reply = jobs.get_nowait()
        except queue.Empty:
            return 0.1
        output = io.StringIO()
        try:
            namespace['RESULT'] = None
            with contextlib.redirect_stdout(output):
                exec(compile(request['code'], request.get('filename', '<live-artist>'), 'exec'), namespace)
            response = {'ok': True, 'result': namespace.get('RESULT'), 'log': output.getvalue()}
        except Exception:
            response = {'ok': False, 'error': traceback.format_exc(), 'log': output.getvalue()}
        reply.put(response)
        return 0.1

    threading.Thread(target=serve, daemon=True).start()
    bpy.app.timers.register(execute_pending, persistent=True)
    bpy.app.driver_namespace['prison_artist_bridge'] = server
    print('Live artist bridge ready on 127.0.0.1:9877')
