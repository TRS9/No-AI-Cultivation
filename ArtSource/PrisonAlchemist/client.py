"""Send a local bpy script to the already-open Blender instance."""
import json
import socket
import sys
from pathlib import Path

path = Path(sys.argv[1]).resolve()
with socket.create_connection(('127.0.0.1', 9877), timeout=15) as connection:
    connection.settimeout(600)
    connection.sendall(json.dumps({'code': path.read_text(encoding='utf-8'), 'filename': str(path)}).encode('utf-8') + b'\n')
    data = bytearray()
    while not data.endswith(b'\n'):
        block = connection.recv(65536)
        if not block:
            break
        data.extend(block)
    response = json.loads(data)
    print(json.dumps(response, indent=2, ensure_ascii=True))
    sys.exit(0 if response.get('ok') else 1)
