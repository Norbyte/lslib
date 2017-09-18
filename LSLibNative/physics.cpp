#include "physics.h"

#if defined(HAS_BULLET)
#pragma managed(push, off)
#include <string>
#include <iostream>
#include <fstream>
#include "BulletWorldImporter/btBulletWorldImporter.h"
#include "BulletCollision/CollisionShapes/btShapeHull.h"
#pragma comment(lib, "BulletCollision.lib")
#pragma comment(lib, "BulletDynamics.lib")
#pragma comment(lib, "BulletWorldImporter.lib")
#pragma comment(lib, "LinearMath.lib")
#pragma managed(pop)

namespace LSLib {
	namespace Native {

		PhysicsAssetExporter::PhysicsAssetExporter()
			: vertices_(nullptr)
		{
		}

		void PhysicsAssetExporter::exportBullet(ExporterOptions ^ options)
		{
			pin_ptr<float> inertia(&options->Inertia[options->Inertia->GetLowerBound(0)]);
			btCollisionShape * shape = nullptr;

			MeshShapeOptions ^ meshOpts = dynamic_cast<MeshShapeOptions ^>(options);
			if (meshOpts)
			{
				pin_ptr<float> vertices(&meshOpts->Vertices[meshOpts->Vertices->GetLowerBound(0)]);
				pin_ptr<int> indices(&meshOpts->Indices[meshOpts->Indices->GetLowerBound(0)]);
				vertices_ = new btTriangleIndexVertexArray(
					meshOpts->Indices->Length / 3, indices, sizeof(int)* 3,
					meshOpts->Vertices->Length, reinterpret_cast<btScalar *>(vertices), sizeof(float)* 3
					);

				switch (meshOpts->Type)
				{
				case MESH_CONCAVE:
					shape = new btBvhTriangleMeshShape(vertices_, true, true);
					break;

				case MESH_CONVEX_HULL:
				{
					auto * hull = new btConvexHullShape();
					shape = hull;

					for (int i = 0; i < meshOpts->Vertices->Length; i++)
						hull->addPoint(*reinterpret_cast<btVector3 *>(&vertices[i * 3]));
					break;
				}

				case MESH_SIMPLIFIED_CONVEX_HULL:
				{
					auto triShape = new btConvexTriangleMeshShape(vertices_);
					btShapeHull * hull = new btShapeHull(triShape);
					btScalar margin = triShape->getMargin();
					hull->buildHull(margin);
					shape = new btConvexHullShape((const btScalar *)hull->getVertexPointer(), hull->numVertices());
					break;
				}

				default:
					throw gcnew Exception("Unsupported physics mesh format");

				}
			}
			else
			{
				SphereShapeOptions ^ sphereOpts = dynamic_cast<SphereShapeOptions ^>(options);
				BoxShapeOptions ^ boxOpts = dynamic_cast<BoxShapeOptions ^>(options);
				if (sphereOpts)
				{
					shape = new btSphereShape(sphereOpts->Radius);
				}
				else if (boxOpts)
				{
					pin_ptr<float> extents(&boxOpts->Extents[boxOpts->Extents->GetLowerBound(0)]);
					shape = createBoxShape(extents);
				}
			}

			if (!shape)
				throw gcnew Exception("Invalid physics shape specification");

			shape->setMargin(options->Shape->Margin);

			pin_ptr<float> translation(&options->Translation[options->Translation->GetLowerBound(0)]);
			btRigidBody * rb = createRigidBody(options->Mass, shape, translation, options->AngularDamping, options->LinearDamping, options->Friction, options->Restitution, inertia);

			int collisionFlags = 0;
			if (options->Flags & EF_STATIC_OBJECT)
				collisionFlags |= btCollisionObject::CF_STATIC_OBJECT;
			if (options->Flags & EF_KINEMATIC_OBJECT)
				collisionFlags |= btCollisionObject::CF_KINEMATIC_OBJECT;
			if (options->Flags & EF_NO_CONTACT_RESPONSE)
				collisionFlags |= btCollisionObject::CF_NO_CONTACT_RESPONSE;
			if (options->Flags & EF_CHARACTER_OBJECT)
				collisionFlags |= btCollisionObject::CF_CHARACTER_OBJECT;
			rb->setCollisionFlags(collisionFlags);

			int rbFlags = 0;
			if (options->Flags & EF_DISABLE_WORLD_GRAVITY)
				rbFlags |= BT_DISABLE_WORLD_GRAVITY;
			rb->setFlags(rbFlags);

			auto outStr = options->OutputPath;
			auto path = msclr::interop::marshal_as<std::string>(outStr);
			exportWorld(rb, path, (options->Flags & EF_STEP_SIMULATION) == EF_STEP_SIMULATION);

			delete shape;
			delete rb;
			delete vertices_;
			vertices_ = nullptr;
		}

		// C++/CLR doesn't really work well with SSE-optimized code / aligned Bullet objects, so we need to move
		// those calls into separate native functions
#pragma managed(push, off)
		void PhysicsAssetExporter::exportWorld(btRigidBody * rb, std::string const & path, bool stepSimulation)
		{
			btDefaultCollisionConfiguration collisionConfiguration;
			btCollisionDispatcher dispatcher(&collisionConfiguration);
			btDbvtBroadphase broadphase;
			btSequentialImpulseConstraintSolver solver;
			btDiscreteDynamicsWorld world(&dispatcher, &broadphase, &solver, &collisionConfiguration);

			world.addCollisionObject(rb);
			if (stepSimulation)
				world.stepSimulation(0.25f);

			btDefaultSerializer serializer(1024 * 1024);
			world.serialize(&serializer);

			std::ofstream bulletFile;
			bulletFile.open(path.c_str(), std::iostream::binary | std::iostream::out);
			if (bulletFile.fail())
				throw std::exception("Failed to open output file");

			int bufferSize = serializer.getCurrentBufferSize();
			const char * buffer = reinterpret_cast<const char *>(serializer.getBufferPointer());
			bulletFile.write(buffer, bufferSize);
			bulletFile.close();
		}

		btRigidBody * PhysicsAssetExporter::createRigidBody(float mass, btCollisionShape * shape, float * translation, float angularDamping,
			float linearDamping, float friction, float restitution, float * inertia)
		{
			btVector3 btInertia(inertia[0], inertia[1], inertia[2]);
			btRigidBody::btRigidBodyConstructionInfo rbInfo(mass, nullptr, shape, btInertia);
			rbInfo.m_angularDamping = angularDamping;
			rbInfo.m_linearDamping = linearDamping;
			rbInfo.m_friction = friction;
			rbInfo.m_restitution = restitution;
			btRigidBody * rb = new btRigidBody(rbInfo);

			btVector3 btTranslation(translation[0], translation[1], translation[2]);
			rb->translate(btTranslation);
			return rb;
		}

		btCollisionShape * PhysicsAssetExporter::createBoxShape(float * extents)
		{
			btVector3 btExtents(extents[0], extents[1], extents[2]);
			return new btBoxShape(btExtents);
		}
#pragma managed(pop)

	}
}
#endif