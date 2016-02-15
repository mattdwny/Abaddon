/** The primative spherical geometry component that is used to traverse a block or terrain
 *
 * TODO: detailed description
 * 
 * @file
 */

using UnityEngine;
using System.Collections;
using System.Linq;
//using System.Diagnostics;

[System.Serializable]
public class SphericalIsoscelesTrapezoid /*TODO: get rid of this in production builds*/ : MonoBehaviour //will become Component
{
	/*TODO: make const*/
	[SerializeField] public SphericalIsoscelesTrapezoid		next; //TODO: add k prefix
	[SerializeField] public SphericalIsoscelesTrapezoid		prev;

	[SerializeField] Vector3								path_center;
	[SerializeField] Vector3								path_normal;

	[SerializeField] Vector3								arc_left;
	[SerializeField] Vector3								arc_right; //CONSIDER: can be dropped

	[SerializeField] Vector3								arc_left_up;
	[SerializeField] Vector3								arc_right_down;
	
	[SerializeField] float									arc_radius;
	[SerializeField] float									arc_angle;
	
	/** Determine if the character (represented by a point) is inside of a trapezoid (extruded by the radius of the player)
	 *  
	 */
	public bool Contains(Vector3 pos, float radius_sine)
	{
		float prod = Vector3.Dot(pos - path_center, path_normal);

		bool bIsAtCorrectElevation = 0 <= prod && prod <= radius_sine;
		bool bLeftContains		   = Vector3.Dot(pos, arc_left_up ) >= 0;
		bool bRightContains		   = Vector3.Dot(pos, arc_right_down) >= 0;
		bool bIsObtuse			   = Vector3.Dot(arc_left, arc_right) <= 0;
		int  nOutOfThree		   = Truth(bLeftContains, bRightContains, bIsObtuse);

		return bIsAtCorrectElevation && nOutOfThree >= 2; //XXX: might even now be wrong
	}

	public Optional<float> Distance(Vector3 to, Vector3 from)
	{
		Optional<float> intersection = Intersect(to, from, 0.01f);
		
		if(intersection.HasValue)
		{
			float t = intersection.Value;
			Vector3 newPos = Evaluate(t, 0.01f);
			return Vector3.Distance(from, newPos);
		}
		
		return new Optional<float>();
	}

	void DrawArc(float height, Color color)
	{
		// draw CoM path
		UnityEditor.Handles.color = color;
		Vector3 adj_center = path_center + path_normal*(height*Mathf.Sin(height));
		float   adj_radius = arc_radius - (height - height*Mathf.Cos(height));
		Vector3 adj_from   = adj_center + arc_left*adj_radius;
		UnityEditor.Handles.DrawWireArc(adj_center, path_normal, adj_from, arc_angle * 180 / Mathf.PI, adj_radius);
		
		//DebugUtility.Print(adj_from.magnitude.ToString(), 100);
	}

	/** return the position of the player based on the circular path
	 *  
	 */
	public Vector3 Evaluate(float t, float height)
	{
		float angle = t / arc_radius;

		float z_height  = height*     Mathf.Sin(height) ;
		float xy_height = height*(1 - Mathf.Cos(height));

		Vector3 x = -arc_left   *(arc_radius + xy_height)*Mathf.Cos(angle); //-arcLeft for "right" is intentional
		Vector3 y =  arc_left_up*(arc_radius + xy_height)*Mathf.Sin(angle);
		Vector3 z =  path_normal*z_height;

		return x + y + z + path_center; 
	}

	/** return the position of the player based on the circular path
	 *  
	 */
	public Vector3 Evaluate(float t) { return Evaluate(t, 0f); }

	/** return the position of the player based on the circular path
	 * 
	 *  return the position of the player based on the circular path
	 *  If the player would go outside of [0, arcCutoffAngle*arcRadius],
	 *  the Trapezoid should transfer control of the player to (prev, next) respectively
	 */
	public static Vector3 Evaluate(ref float t, float height, ref SphericalIsoscelesTrapezoid seg)
	{
		if(t > seg.arc_angle*seg.arc_radius)
		{
			t -= seg.arc_angle*seg.arc_radius;
			seg = seg.next;
			return Evaluate(ref t, 0.01f, ref seg);
		}
		if(t < 0)
		{
			t += seg.prev.arc_angle*seg.prev.arc_radius;
			seg = seg.prev;
			return Evaluate(ref t, 0.01f, ref seg);
		}
		
		return seg.Evaluate(t, height);
	}

	public Vector3 EvaluateNormal(Vector3 pos, Vector3 right)
	{
		return Vector3.Cross(right, pos);
	}

	public Vector3 EvaluateRight(float t)
	{
		float angle = t / arc_radius;
		return arc_left_up*Mathf.Cos(angle) + arc_left*Mathf.Sin(angle);
	}

	/** Recompute the orientation of a SphericalIsoscelesTrapezoid
	 * 
	 *  Destroys all information other than prev, next. Replaces this information with the information for traversing
	 *      the top of a SphericalIsoscelesTrapezoid on a unit sphere.
	 * 
	 * @param left_edge: the left-bottom point (left implies it is the 1st point when enumerated clockwise for concave objects,
	 * 		  bottom implies it is the position of the player's feet)
	 * @param right_edge: the right-bottom point (right implies it is the 2nd point when enumerated clockwise for concave objects,
	 * 		  bottom implies it is the position of the player's feet)
	 * @param normal: the normal plane that intersects lhs and rhs and forms the walking path for the players center
	 * 		  of mass, sign matters because it indicates which direction is up for calculating the center of mass.
	 * 
	 * @example Initialize(Vector3(0,0,1),Vector3(1,0,0), Vector3(0,1,0)) will initialize a Spherical Isosceles Trapezoid
	 *          that is a great circle for the feet positions, a large lesser circle for the center of mass position,
	 *          with a 90 degree arc going from forwards to right and a normal going in the positive y-direction.
	 */
	public void Initialize(Vector3 left_edge, Vector3 right_edge, Vector3 normal)
	{
		//DebugUtility.Assert(Mathf.Approximately(Vector3.Dot(right_edge - left_edge, normal), 0),
		//                    "SphericalIsoscelesTrapezoid: Initialize: failed assert");
		
		path_normal = normal;
		path_center = normal*Vector3.Dot(left_edge, normal); //or right_edge

		arc_left  = (left_edge  - path_center).normalized;
		arc_right = (right_edge - path_center).normalized;

		arc_left_up    =  Vector3.Cross(path_normal, arc_left);
		arc_right_down = -Vector3.Cross(path_normal, arc_right);
		
		arc_radius = (left_edge - path_center).magnitude; //or right_edge
		
		arc_angle = Vector3.Angle(arc_left, arc_right) * Mathf.PI / 180;

		if(Vector3.Dot(arc_left_up, arc_right) <= 0)
		{
			arc_angle += Mathf.PI;
		}
	}

	Vector3 MaxGradient(Vector3 desired)
	{
		Vector3 max_gradient = Vector3.zero;
		float max_product = Mathf.NegativeInfinity;

		/** if we don't do this, calculations for an arc with angle 2*PI become ambiguous because left == right
		 */ 
		for(int quadrant = 0; quadrant < 4; ++quadrant)
		{
			float left  = arc_angle*arc_radius*( quadrant       / 4f); //get beginning of quadrant i.e. 0.00,0.25,0.50,0.75
			float right = arc_angle*arc_radius*((quadrant + 1 ) / 4f); //get    end    of quadrant i.e. 0.25,0.50,0.75,1.00

			Debug.Log(quadrant + ": from " + left + " to " + right);

			float left_product  = Vector3.Dot(Evaluate(left) , desired); //find the correlation factor between left and the desired direction
			float right_product = Vector3.Dot(Evaluate(right), desired);

			/** this is basically a binary search
			 * 
			 *  1) take the left and right vectors and compute their dot products with the desired direction.
			 *  2) take the lesser dot product and ignore that half of the remaining arc
			 */
			for(int iteration = 0; iteration < 8*sizeof(float); ++iteration) //because we are dealing with floats, more precision could help (or hurt?)
			{
				float midpoint = (left + right) / 2;
				if(left_product < right_product) //is the right vector closer to the desired direction?
				{
					left = midpoint; //throw out the left half if the right vector is closer
					left_product = Vector3.Dot(Evaluate(left), desired);
				}
				else
				{
					right = midpoint; //throw out the right half if the left vector is closer
					right_product = Vector3.Dot(Evaluate(right), desired);
				}
			}

			/** figure out if this quadrant contains a larger gradient
			 */
			if(max_product < right_product)
			{
				max_gradient = Evaluate(right);
				max_product = right_product;
			}
			if(max_product < left_product)
			{
				max_gradient = Evaluate(left);
				max_product = left_product;
			}
		}
		return max_gradient;
	}

	/** Find the point of collision as a parameterization of a circle.
	 *  
	 */
	public Optional<float> Intersect(Vector3 to, Vector3 from, float height) //TODO: FIXME: UNJANKIFY
	{
		Vector3 right  = Vector3.Cross(from, to);
		Vector3 secant = Vector3.Cross(path_normal, right);
		
		if(Vector3.Dot(secant, from) < 0f) secant *= -1; //TODO: check

		secant.Normalize();

		Vector3 adj_center = path_center + path_normal*(height*Mathf.Sin(height)); //TODO: - for normals towards the origin
		float   adj_radius = arc_radius - (height - height*Mathf.Cos(height)); //TODO: + for normals pointing towards the origin 

		Vector3 intersection = adj_center + secant*adj_radius;

		float x = Vector3.Dot(intersection, -arc_left   ) / adj_radius;
		float y = Vector3.Dot(intersection,  arc_left_up) / adj_radius;
		
		float angle = Mathf.Atan2(y,x);

		if(angle < 0)
		{
			angle += 2*Mathf.PI;
		}

		if(angle <= arc_angle)
		{
			return angle*arc_radius; //there needs to be a mechanism for changing speed based on radius...
		}
		return new Optional<float>();
	}
	
	private void OnDrawGizmos() //TODO: get rid of this in production builds
	{
		// draw floor path
		DrawArc(0.0f, Color.black);

		// draw CoM path
		DrawArc(0.1f, Color.white);
	}

	/** Create a AABB that perfectly contains a circular arc
	 * 
	 *  TODO: detailed description and math link
	 * 
	 *  TODO: Ex. 
	 * 
	 *  @param collider the box collider that will be altered to contain the SphericalIsoscelesTrapezoid
	 */
	public void RecalculateAABB(BoxCollider collider)
	{
		float x_min = MaxGradient(Vector3.left   ).x;
		float x_max = MaxGradient(Vector3.right  ).x;
		float y_min = MaxGradient(Vector3.down   ).y;
		float y_max = MaxGradient(Vector3.up     ).y;
		float z_min = MaxGradient(Vector3.back   ).z;
		float z_max = MaxGradient(Vector3.forward).z;

		collider.center = new Vector3((x_max + x_min) / 2,
		                              (y_max + y_min) / 2,
		                              (z_max + z_min) / 2);

		collider.size   = new Vector3( x_max - x_min,
									   y_max - y_min,
									   z_max - z_min);
	}

	/** Counts the number of booleans that are true in a comma separated list of booleans
	 * 
	 *  credit: http://stackoverflow.com/questions/377990/elegantly-determine-if-more-than-one-boolean-is-true
	 */
	public static int Truth(params bool[] booleans)
	{
		return booleans.Count(b => b);
	}
}