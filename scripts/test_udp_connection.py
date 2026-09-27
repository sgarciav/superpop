#!/usr/bin/env python3
"""
Simple UDP Connection Tester
Tests if UDP packets are being sent/received correctly between 
Python streamer and Unity receiver.
"""

import socket
import struct
import sys
import time

def test_sender():
    """Test sending UDP packets"""
    print("\n[UDP Sender Test]")
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    
    try:
        # Create sample packet (same format as mediapipe_udp_streamer.py)
        packet = bytearray()
        packet.extend(b'POSE')  # Header
        packet.append(3)  # 3 joints for testing
        
        # Joint 11: Left shoulder at (0.1, -0.2, 0.5)
        packet.append(11)
        packet.extend(struct.pack('fff', 0.1, -0.2, 0.5))
        
        # Joint 15: Left wrist at (0.3, -0.5, 0.8)
        packet.append(15)
        packet.extend(struct.pack('fff', 0.3, -0.5, 0.8))
        
        # Joint 16: Right wrist at (-0.3, -0.5, 0.8)
        packet.append(16)
        packet.extend(struct.pack('fff', -0.3, -0.5, 0.8))
        
        # Send it
        sock.sendto(bytes(packet), ("127.0.0.1", 5005))
        print(f"✓ Sent {len(packet)} bytes to 127.0.0.1:5005")
        print(f"  Packet hex: {packet.hex()}")
        print(f"  Packet size breakdown: header(4) + count(1) + 3×(index(1)+xyz(12)) = {4+1+3*13} bytes")
        
    except Exception as e:
        print(f"✗ Error sending: {e}")
    finally:
        sock.close()

def test_receiver():
    """Test receiving UDP packets"""
    print("\n[UDP Receiver Test]")
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    
    try:
        sock.bind(("127.0.0.1", 5005))
        sock.settimeout(5)  # 5 second timeout
        print("✓ Listening on 127.0.0.1:5005...")
        
        # Wait for packet
        data, addr = sock.recvfrom(1024)
        print(f"✓ Received {len(data)} bytes from {addr}")
        print(f"  Packet hex: {data.hex()}")
        
        # Parse it
        if len(data) >= 5:
            header = data[0:4].decode('ascii', errors='ignore')
            count = data[4]
            print(f"  Header: '{header}'")
            print(f"  Joint count: {count}")
            
            offset = 5
            for i in range(count):
                if offset + 13 <= len(data):
                    joint_idx = data[offset]
                    x, y, z = struct.unpack('fff', data[offset+1:offset+13])
                    print(f"    Joint {joint_idx}: ({x:.3f}, {y:.3f}, {z:.3f})")
                    offset += 13
        
    except socket.timeout:
        print("✗ Timeout waiting for packet")
        print("  Make sure mediapipe_udp_streamer.py is running!")
    except Exception as e:
        print(f"✗ Error receiving: {e}")
    finally:
        sock.close()

def check_port_availability():
    """Check if port 5005 is available"""
    print("\n[Port Availability Check]")
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    
    try:
        sock.bind(("127.0.0.1", 5005))
        print("✓ Port 5005 is available")
        sock.close()
    except OSError:
        print("✗ Port 5005 is in use (probably by mediapipe_udp_streamer.py)")
        print("  This is expected if the streamer is running!")

def main():
    print("╔════════════════════════════════════════════╗")
    print("║    UDP Connection Test Utility             ║")
    print("║    For MediaPipe UDP Bridge Testing        ║")
    print("╚════════════════════════════════════════════╝")
    
    if len(sys.argv) > 1:
        mode = sys.argv[1].lower()
        
        if mode == "send":
            test_sender()
        elif mode == "recv":
            test_receiver()
        elif mode == "check":
            check_port_availability()
        else:
            print(f"Unknown mode: {mode}")
            print_usage()
    else:
        # Run full test
        print("\n" + "="*50)
        print("Running Full Connection Test")
        print("="*50)
        
        check_port_availability()
        test_sender()
        print("\nNote: To test receiving, run this in another terminal:")
        print("  python scripts/test_udp_connection.py recv")
        print("\nAnd keep mediapipe_udp_streamer.py running.")

def print_usage():
    print("\nUsage: python test_udp_connection.py [mode]")
    print("\nModes:")
    print("  send  - Send a test packet to localhost:5005")
    print("  recv  - Listen for packets on localhost:5005 (5s timeout)")
    print("  check - Check if port 5005 is available")
    print("  (no args) - Run full test")

if __name__ == "__main__":
    main()
