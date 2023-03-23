using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.CharacterController.RuntimeTests
{
    public struct TestFixedInputEvent
    {
        private byte _wasEverSet;
        private uint _lastSetTick;

        public void Set(uint tick)
        {
            _lastSetTick = tick;
            _wasEverSet = 1;
        }

        public bool IsSet(uint tick)
        {
            if (_wasEverSet == 1)
            {
                return tick == _lastSetTick;
            }

            return false;
        }
    }
}