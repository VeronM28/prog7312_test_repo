// DataStructures/ServiceAreaGraph.cs

using prog7212_poe_part1_V1.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace prog7212_poe_part1_V1.DataStructures
{
    public class ServiceNode
    {
        public string AreaName { get; set; }
        public List<ServiceRequestModel> Requests { get; set; } = new List<ServiceRequestModel>();
        public double CenterLat { get; set; }
        public double CenterLng { get; set; }
    }

    public class ServiceAreaGraph
    {
        private Dictionary<string, ServiceNode> nodes;
        private Dictionary<string, List<(string neighbor, double distance)>> adjacencyList;

        public ServiceAreaGraph()
        {
            nodes = new Dictionary<string, ServiceNode>();
            adjacencyList = new Dictionary<string, List<(string, double)>>();
        }

        public void AddArea(string areaName, double lat, double lng)
        {
            nodes[areaName] = new ServiceNode
            {
                AreaName = areaName,
                CenterLat = lat,
                CenterLng = lng
            };
            adjacencyList[areaName] = new List<(string, double)>();
        }

        public void AddConnection(string area1, string area2)
        {
            var node1 = nodes[area1];
            var node2 = nodes[area2];

            double distance = CalculateDistance(node1.CenterLat, node1.CenterLng,
                                             node2.CenterLat, node2.CenterLng);

            adjacencyList[area1].Add((area2, distance));
            adjacencyList[area2].Add((area1, distance));
        }

        public void AddRequestToArea(string areaName, ServiceRequestModel request)
        {
            if (nodes.ContainsKey(areaName))
            {
                nodes[areaName].Requests.Add(request);
            }
        }

        public List<string> FindOptimalRoute(string startArea)
        {
            var visited = new HashSet<string>();
            var route = new List<string>();

            DFS(startArea, visited, route);
            return route;
        }

        private void DFS(string current, HashSet<string> visited, List<string> route)
        {
            visited.Add(current);
            route.Add(current);

            var neighbors = adjacencyList[current]
                .Where(n => !visited.Contains(n.neighbor))
                .OrderBy(n => n.distance)
                .ToList();

            foreach (var neighbor in neighbors)
            {
                if (!visited.Contains(neighbor.neighbor))
                {
                    DFS(neighbor.neighbor, visited, route);
                }
            }
        }

        public List<ServiceRequestModel> GetRequestsInOptimalOrder(string startArea)
        {
            var optimalRoute = FindOptimalRoute(startArea);
            var orderedRequests = new List<ServiceRequestModel>();

            foreach (var area in optimalRoute)
            {
                orderedRequests.AddRange(nodes[area].Requests
                    .Where(r => r.Status == RequestStatus.InProgress || r.Status == RequestStatus.Submitted)
                    .OrderByDescending(r => r.PriorityScore));
            }

            return orderedRequests;
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for distance calculation
            const double R = 6371; // Earth's radius in kilometers
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        // Explanation of Graph Role:
        // The Service Area Graph models geographical relationships between service areas.
        // It enables optimal routing for service teams by finding the most efficient path
        // through connected areas, reducing travel time and costs.
        // Example: When dispatching maintenance teams, the graph ensures they follow the most
        // efficient route between service locations, considering geographical proximity.
    }
}