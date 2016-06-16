#pragma once

#if defined(HAS_BULLET)
#include <msclr/marshal_cppstd.h>
#pragma managed(push, off)
#include <btBulletDynamicsCommon.h>
#pragma managed(pop)

using namespace System;
using namespace System::Collections::Generic;

namespace LSLib {
	namespace Native {

		enum ExporterFlags
		{
			EF_STATIC_OBJECT = 1,
			EF_KINEMATIC_OBJECT = 2,
			EF_NO_CONTACT_RESPONSE = 4,
			EF_CHARACTER_OBJECT = 8,
			EF_DISABLE_WORLD_GRAVITY = 16,
			EF_STEP_SIMULATION = 32
		};

		enum MeshType
		{
			MESH_CONCAVE,
			MESH_CONVEX_HULL,
			MESH_SIMPLIFIED_CONVEX_HULL
		};

		public ref class ShapeOptions
		{
		public:
			float Margin;
		};

		public ref class SphereShapeOptions : public ShapeOptions
		{
		public:
			float Radius;
		};

		public ref class BoxShapeOptions : public ShapeOptions
		{
		public:
			array<float> ^ Extents;
		};

		public ref class MeshShapeOptions : public ShapeOptions
		{
		public:
			MeshType Type;
			array<float> ^ Vertices;
			array<int> ^ Indices;
		};

		public ref class ExporterOptions
		{
		public:
			ExporterFlags Flags;
			String ^ OutputPath;
			array<float> ^ Translation;
			array<float> ^ Inertia;
			float Mass;
			float LinearDamping;
			float AngularDamping;
			float Friction;
			float Restitution;
			ShapeOptions ^ Shape;
		};

		public class PhysicsAssetExporter
		{
		public:
			PhysicsAssetExporter();
			void exportBullet(ExporterOptions ^ options);

		private:
			btTriangleIndexVertexArray * vertices_;

			void exportWorld(btRigidBody * rb, std::string const & path, bool stepSimulation);
			btRigidBody * createRigidBody(float mass, btCollisionShape * shape, float * translation, float angularDamping,
				float linearDamping, float friction, float restitution, float * inertia);
			// class btCollisionShape * createCollisionShape(ShapeOptions ^ options);
			btCollisionShape * createBoxShape(float * extents);
		};

	}
}
#endif