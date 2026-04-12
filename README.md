# LRU-Cache
LRU Cache will evict the least recently used item

Overview
This repository contains implementations of an LRU (Least Recently Used) Cache in multiple languages, demonstrating how to evict a least recently used item once capacity is reached.
The csharp and python folders show an implementation just using an int as key and value, the project is written in C# which uses generics to allow a user to store any data type they wish.
The LRU cache supports:\
get(key) → O(1)\
put(key, value) → O(1)\
Implementations

Python
Version included:
1. Built-in (OrderedDict)

Uses Python’s standard library
Clean and production-ready
Maintains insertion order internally

C#
Uses:\
Dictionary<TKey, LinkedListNode<T>>

LinkedList<T>\
Achieves O(1) operations\
How It Works\
Key Idea\
HashMap -> fast lookup\
Linked List -> track usage order\
Flow

Access item -> move to most recently used (tail)

Insert item:

If exists -> update + move\
If full -> remove least recently used (head)\
Complexity\
Operation	Time	Space\
get	O(1)	O(n)\
put	O(1)	O(n)

Notes\
Python OrderedDict version is optimal for real-world use
