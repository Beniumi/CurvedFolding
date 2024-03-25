using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeSceneView
{
    public class LimitedStack<T>
    {
        private readonly int limit;
        private readonly LinkedList<T> stack;

        public int Count => stack.Count;

        public LimitedStack(int limit)
        {
            this.limit = limit;
            stack = new LinkedList<T>();
        }

        public void Clear()
        {
            stack.Clear();
        }

        public void Push(T element)
        {
            stack.AddLast(element);
            if (Count > limit)
                stack.RemoveFirst();
        }

        public T Pop()
        {
            T element = stack.Last.Value;
            stack.RemoveLast();
            return element;
        }

        public T Peek() => stack.Last.Value;

        public bool SafePop(out T element)
        {
            LinkedListNode<T> node = stack.Last;
            if (node == null)
            {
                element = default(T);
                return false;
            }
            element = node.Value;
            stack.RemoveLast();
            return true;
        }

        public bool SafePeek(out T element)
        {
            LinkedListNode<T> node = stack.Last;
            if (node == null)
            {
                element = default(T);
                return false;
            }
            element = node.Value;
            return true;
        }
    }

}