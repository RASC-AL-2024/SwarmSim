// 
// using UnityEngine;
// 
// public class CircularLinkedList
// {
//     public class Node
//     {
//         public RoverNode Data { get; set; }
//         public Node Next { get; set; }
// 
//         public Node(RoverNode data)
//         {
//             this.Data = data;
//             this.Next = null;
//         }
//     }
// 
//     public Node head;
//     public bool should_repeat;
// 
//     public CircularLinkedList()
//     {
//         head = null;
//         should_repeat = true;
//     }
// 
//     public void Add(RoverNode data)
//     {
//         Node newNode = new Node(data);
// 
//         if (head == null)
//         {
//             head = newNode;
//             if(should_repeat)
//             {
//                 head.Next = head;
//             } else
//             {
//                 head.Next = null;
//             }
//         }
//         else
//         {
//             Node current = head;
//             while (current.Next != head && current.Next != null)
//             {
//                 current = current.Next;
//             }
//             current.Next = newNode;
// 
//             if(should_repeat)
//             {
//                 newNode.Next = head;
//             } else
//             {
//                 newNode.Next = null;
//             }
//         }
//     }
// 
//     public bool Step(Vector2 pos, float time)
//     {
//         float time_delta = time - head.Data.timestamp;
//         return head.Data.goal.Check(pos, time_delta);
//     }
// 
//     public void AdvanceAndSet(float curr_time)
//     {
//         head = head.Next;
//         if(head != null)
//         {
//             head.Data.timestamp = curr_time;
//             head.Data.goal.Generate();
//         }
//     }
// }
