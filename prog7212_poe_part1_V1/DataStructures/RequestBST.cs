// DataStructures/RequestBST.cs

using prog7212_poe_part1_V1.Models;
using System;
using System.Collections.Generic;

namespace prog7212_poe_part1_V1.DataStructures
{
    public class BSTNode
    {
        public ServiceRequestModel Request { get; set; }
        public BSTNode Left { get; set; }
        public BSTNode Right { get; set; }

        public BSTNode(ServiceRequestModel request)
        {
            Request = request;
        }
    }

    public class RequestBST
    {
        private BSTNode root;
        private int count;

        public int Count => count;

        public void Insert(ServiceRequestModel request)
        {
            root = InsertRec(root, request);
            count++;
        }

        private BSTNode InsertRec(BSTNode node, ServiceRequestModel request)
        {
            if (node == null)
            {
                return new BSTNode(request);
            }

            // Compare by creation date for chronological ordering
            if (request.CreatedDate < node.Request.CreatedDate)
            {
                node.Left = InsertRec(node.Left, request);
            }
            else
            {
                node.Right = InsertRec(node.Right, request);
            }

            return node;
        }

        public List<ServiceRequestModel> InOrderTraversal()
        {
            var result = new List<ServiceRequestModel>();
            InOrderRec(root, result);
            return result;
        }

        private void InOrderRec(BSTNode node, List<ServiceRequestModel> result)
        {
            if (node != null)
            {
                InOrderRec(node.Left, result);
                result.Add(node.Request);
                InOrderRec(node.Right, result);
            }
        }

        public ServiceRequestModel SearchByDate(DateTime targetDate)
        {
            return SearchByDateRec(root, targetDate);
        }

        private ServiceRequestModel SearchByDateRec(BSTNode node, DateTime targetDate)
        {
            if (node == null) return null;

            if (node.Request.CreatedDate.Date == targetDate.Date)
                return node.Request;

            if (targetDate < node.Request.CreatedDate)
                return SearchByDateRec(node.Left, targetDate);
            else
                return SearchByDateRec(node.Right, targetDate);
        }

        // Explanation of BST Role:
        // The Binary Search Tree efficiently organizes service requests by creation date.
        // This allows for O(log n) search operations when looking for requests from specific dates.
        // Example: When an admin needs to find all requests from a particular day, the BST enables
        // efficient range queries and chronological ordering without sorting the entire dataset.
    }
}