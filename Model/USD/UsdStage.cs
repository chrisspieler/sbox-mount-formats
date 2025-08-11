namespace Duccsoft.Formats.Usd;

public class UsdStage : IModelResourceFile
{
	public UsdStage( List<UsdPrim> prims )
	{
		_prims = prims;
	}

	public IReadOnlyList<UsdPrim> Prims => _prims;
	private readonly List<UsdPrim> _prims;

	public Model LoadModel()
	{
		var meshPrims = Prims.Where( p => p.Type == "Mesh" ).ToArray();
		if ( meshPrims.Length < 1 )
		{
			Log.Info( $"No meshes were found." );
			return Model.Cube;
		}

		var meshes = new Mesh[meshPrims.Length];
		for ( int i = 0; i < meshPrims.Length; i++ )
		{
			var meshPrim = meshPrims[i];
			if ( !meshPrim.Attributes["faceVertexIndices"].TryGetValue<int[]>( out var faceVertexIndices ) ) { continue; }
			if ( !meshPrim.Attributes["normals"].TryGetValue<Vector3[]>( out var normals ) ) { continue; }
			if ( !meshPrim.Attributes["points"].TryGetValue<Vector3[]>( out var points ) ) { continue; }
			if ( !meshPrim.Attributes["primvars:st"].TryGetValue<Vector2[]>( out var texcoords ) ) { continue; }
			// TODO: Get primvars:st:indices if available, and use that as texture indices.

			Vector3[] extents = null;
			meshPrim.GetAttribute( "extent" )?.TryGetValue( out extents );

			var mesh = new Mesh( Material.FromShader( "shaders/complex.shader" ) );
			// TODO: Get faceVertexCounts, and triangulate any quads rather than using faceVertexIndices as-is.
			mesh.CreateIndexBuffer( faceVertexIndices.Length, faceVertexIndices );
			var vertices = new Vertex[points.Length];
			for ( int j = 0; j < vertices.Length; j++ )
			{
				vertices[j] = new Vertex()
				{
					Color = Color.White,
					// TODO: Apply the Xform hierarchy to the positions, normals, and tangents.
					Position = points[j],
					Normal = normals[j],
					// TODO: Recalculate tangents.
					Tangent = new Vector4( 1, 0, 0, 0 ),
					TexCoord0 = new Vector4( texcoords[j].x, texcoords[j].y, 0, 0 ),
					TexCoord1 = Vector4.Zero
				};
			}
			mesh.CreateVertexBuffer<Vertex>( vertices.Length, Vertex.Layout, vertices );
			mesh.SetIndexRange( 0, faceVertexIndices.Length );
			// TODO: Recalculate bounds.
			mesh.Bounds = extents is null
				? BBox.FromPositionAndSize( Vector3.Zero, 64f )
				: new BBox( extents[0], extents[1] );
			meshes[i] = mesh;
		}

		return new ModelBuilder()
			// TODO: Add a trace mesh so that the mesh may be clicked in the editor.
			.AddMeshes( meshes )
			// TODO: Set the name to a file path so that the mesh path will be persisted in s&box scenes.
			.WithName( "mesh" )
			.Create();
	}
}
