#include "PhysicsTool.h"
#include <unordered_map>

#define PR(name, expr, def) {auto _v = (expr); if (ExportAllProperties || !(_v == (def))) { ExportProperty(ele, #name, _v); }}
#define P(name, expr) ExportProperty(ele, #name, (expr))
#define P_BOUNDED(name, expr, bound) {auto _v = (expr); if (ExportAllProperties || _v >= (bound)) { if (_v < bound) ExportProperty(ele, #name, _v); else ExportProperty(ele, #name, "Unbounded"); }}
#define PFLAG(name, expr, flag) {auto _v = (expr); if (ExportAllProperties || (_v & flag) == flag) { ExportProperty(ele, #name, (_v & flag) == flag); }}

class PhysXExporter
{
public:
    bool ExportAllProperties{ true };
    std::unordered_map<PxMaterial*, uint32_t> materials_;

    TiXmlDocument* Export(PxCollection& collection)
    {
        auto doc = new TiXmlDocument();
        doc->InsertEndChild(TiXmlDeclaration("1.0", "UTF-8", ""));
        auto root = doc->InsertEndChild(TiXmlElement("BG3Physics"));

        for (uint32_t i = 0; i < collection.getNbObjects(); i++) {
            auto& obj = collection.getObject(i);
            ExportTopLevel(*root, obj);
        }

        return doc;
    }

    template <class T>
    void ExportProperty(TiXmlNode& ele, char const* name, T value)
    {
        auto& prop = *ele.InsertEndChild(TiXmlElement(name));
        prop.InsertEndChild(TiXmlText(std::to_string(value)));
    }

    template <>
    void ExportProperty<bool>(TiXmlNode& ele, char const* name, bool obj)
    {
        auto& prop = *ele.InsertEndChild(TiXmlElement(name));
        prop.InsertEndChild(TiXmlText(obj ? "true" : "false"));
    }

    template <>
    void ExportProperty<PxReal>(TiXmlNode& ele, char const* name, PxReal obj)
    {
        auto& prop = *ele.InsertEndChild(TiXmlElement(name));
        char val[32];
        std::snprintf(val, sizeof(val), "%.8f", obj);
        prop.InsertEndChild(TiXmlText(val));
    }

    template <>
    void ExportProperty<char const*>(TiXmlNode& ele, char const* name, char const* obj)
    {
        auto& prop = *ele.InsertEndChild(TiXmlElement(name));
        prop.InsertEndChild(TiXmlText(obj));
    }

    template <>
    void ExportProperty<PxVec3>(TiXmlNode& parent, char const* name, PxVec3 obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(X, obj.x, 0.0f);
        PR(Y, obj.y, 0.0f);
        PR(Z, obj.z, 0.0f);
    }

    template <>
    void ExportProperty<PxQuat>(TiXmlNode& parent, char const* name, PxQuat obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(X, obj.x, 0.0f);
        PR(Y, obj.y, 0.0f);
        PR(Z, obj.z, 0.0f);
        PR(W, obj.w, 1.0f);
    }

    template <>
    void ExportProperty<PxTransform>(TiXmlNode& parent, char const* name, PxTransform obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(Position, obj.p, PxVec3());
        PR(Rotation, obj.q, PxQuat());
    }

    template <>
    void ExportProperty<PxMeshScale>(TiXmlNode& parent, char const* name, PxMeshScale obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(Scale, obj.scale, PxVec3());
        PR(Rotation, obj.rotation, PxQuat());
    }

    template <>
    void ExportProperty<PxJointLinearLimit>(TiXmlNode& parent, char const* name, PxJointLinearLimit obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        P_BOUNDED(Value, obj.value, 3.4e+37f); // PX_MAX_F32
        PR(Restitution, obj.restitution, 0.0f);
        PR(BounceThreshold, obj.bounceThreshold, 0.0f);
        PR(Stiffness, obj.stiffness, 0.0f);
        PR(Damping, obj.damping, 0.0f);
        PR(ContactDistance, obj.contactDistance, 0.0f);
    }

    template <>
    void ExportProperty<PxD6Motion::Enum>(TiXmlNode& parent, char const* name, PxD6Motion::Enum obj)
    {
        constexpr char const* kNames[] = {"Locked", "Limited", "Free"};

        auto& prop = *parent.InsertEndChild(TiXmlElement(name));
        prop.InsertEndChild(TiXmlText(kNames[(uint32_t)obj]));
    }

    template <>
    void ExportProperty<PxJointLinearLimitPair>(TiXmlNode& parent, char const* name, PxJointLinearLimitPair obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(Lower, obj.lower, -PX_MAX_F32/3);
        PR(Upper, obj.upper, PX_MAX_F32/3);
        PR(Restitution, obj.restitution, 0.0f);
        PR(BounceThreshold, obj.bounceThreshold, 0.0f);
        PR(Stiffness, obj.stiffness, 0.0f);
        PR(Damping, obj.damping, 0.0f);
        PR(ContactDistance, obj.contactDistance, 0.0f);
    }

    template <>
    void ExportProperty<PxJointAngularLimitPair>(TiXmlNode& parent, char const* name, PxJointAngularLimitPair obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(Lower, obj.lower, -(float)M_PI/2.0f);
        PR(Upper, obj.upper, (float)M_PI/2.0f);
        PR(Restitution, obj.restitution, 0.0f);
        PR(BounceThreshold, obj.bounceThreshold, 0.0f);
        PR(Stiffness, obj.stiffness, 0.0f);
        PR(Damping, obj.damping, 0.0f);
        PR(ContactDistance, obj.contactDistance, 0.0f);
    }

    template <>
    void ExportProperty<PxJointLimitCone>(TiXmlNode& parent, char const* name, PxJointLimitCone obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(YAngle, obj.yAngle, (float)M_PI/2.0f);
        PR(ZAngle, obj.zAngle, (float)M_PI/2.0f);
        PR(Restitution, obj.restitution, 0.0f);
        PR(BounceThreshold, obj.bounceThreshold, 0.0f);
        PR(Stiffness, obj.stiffness, 0.0f);
        PR(Damping, obj.damping, 0.0f);
        PR(ContactDistance, obj.contactDistance, 0.0f);
    }

    template <>
    void ExportProperty<PxJointLimitPyramid>(TiXmlNode& parent, char const* name, PxJointLimitPyramid obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        PR(YAngleMin, obj.yAngleMin, -(float)M_PI/2.0f);
        PR(YAngleMax, obj.yAngleMax, (float)M_PI/2.0f);
        PR(ZAngleMin, obj.zAngleMin, -(float)M_PI/2.0f);
        PR(ZAngleMax, obj.zAngleMax, (float)M_PI/2.0f);
        PR(Restitution, obj.restitution, 0.0f);
        PR(BounceThreshold, obj.bounceThreshold, 0.0f);
        PR(Stiffness, obj.stiffness, 0.0f);
        PR(Damping, obj.damping, 0.0f);
        PR(ContactDistance, obj.contactDistance, 0.0f);
    }

    template <>
    void ExportProperty<PxD6JointDrive>(TiXmlNode& parent, char const* name, PxD6JointDrive obj)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement(name));
        P_BOUNDED(ForceLimit, obj.forceLimit, 3.4e+37f); // PX_MAX_F32
        PFLAG(IsAcceleration, obj.flags, PxD6JointDriveFlag::eACCELERATION);
        PR(Stiffness, obj.stiffness, 0.0f);
        PR(Damping, obj.damping, 0.0f);
    }

    void Export(TiXmlNode& parent, PxMaterial& obj)
    {
        auto it = materials_.find(&obj);
        if (it != materials_.end()) return;

        auto index = (PxU32)materials_.size();
        materials_.insert(std::make_pair(&obj, index));

        auto& ele = *parent.InsertEndChild(TiXmlElement("Material"));

        P(Index, index);
        PR(StaticFriction, obj.getStaticFriction(), 1.0f);
        PR(DynamicFriction, obj.getDynamicFriction(), 1.0f);
        PR(Restitution, obj.getRestitution(), 0.0f);
    }

    void ExportProperties(TiXmlNode& ele, PxSphereGeometry& o)
    {
        ExportProperty(ele, "Type", "Sphere");
        ExportProperty(ele, "Radius", o.radius);
    }

    void ExportProperties(TiXmlNode& ele, PxCapsuleGeometry& o)
    {
        ExportProperty(ele, "Type", "Capsule");
        ExportProperty(ele, "Radius", o.radius);
        ExportProperty(ele, "HalfHeight", o.halfHeight);
    }

    void ExportProperties(TiXmlNode& ele, PxBoxGeometry& o)
    {
        ExportProperty(ele, "Type", "Box");
        ExportProperty(ele, "HalfExtents", o.halfExtents);
    }

    void ExportProperties(TiXmlNode& ele, PxConvexMeshGeometry& o)
    {
        ExportProperty(ele, "Type", "ConvexMesh");
        ExportProperty(ele, "Scale", o.scale);
        // s << "\t" "MeshFlags: " << (uint32_t)o.meshFlags << std::endl; - Always 0

        Export(ele, *o.convexMesh);
    }

    void ExportProperties(TiXmlNode& ele, PxTriangleMeshGeometry& o)
    {
        ExportProperty(ele, "Type", "TriangleMesh");
        ExportProperty(ele, "Scale", o.scale);
        // s << "\t" "MeshFlags: " << (uint32_t)o.meshFlags << std::endl; - Always 0

        Export(ele, *o.triangleMesh);
    }

    void Export(TiXmlNode& parent, PxGeometry& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("Geometry"));

        switch (o.getType())
        {
        case PxGeometryType::eSPHERE: ExportProperties(ele, static_cast<PxSphereGeometry&>(o)); break;
        case PxGeometryType::eCAPSULE: ExportProperties(ele, static_cast<PxCapsuleGeometry&>(o)); break;
        case PxGeometryType::eBOX: ExportProperties(ele, static_cast<PxBoxGeometry&>(o)); break;
        case PxGeometryType::eCONVEXMESH: ExportProperties(ele, static_cast<PxConvexMeshGeometry&>(o)); break;
        case PxGeometryType::eTRIANGLEMESH: ExportProperties(ele, static_cast<PxTriangleMeshGeometry&>(o)); break;

        case PxGeometryType::ePLANE:
        case PxGeometryType::eHEIGHTFIELD:
        default:
            std::cout << "WARNING: Unsupported geometry type: " << o.getType() << std::endl;
            break;
        }
    }

    void Export(TiXmlNode& parent, PxShape& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("Shape"));

        if (o.getNbMaterials() != 1) {
            throw std::runtime_error("Only 1 material per shape is supported");
        }

        PxMaterial* material;
        o.getMaterials(&material, 1, 0);
        auto matIt = materials_.find(material);
        if (matIt == materials_.end()) throw std::runtime_error("Shape references unknown material object");

        ExportProperty(ele, "Name", o.getName());
        P(MaterialIndex, matIt->second);
        PR(LocalPose, o.getLocalPose(), PxTransform());
        PR(ContactOffset, o.getContactOffset(), 0.02f);
        PR(RestOffset, o.getRestOffset(), 0.0f);
        PR(TorsionalPatchRadius, o.getTorsionalPatchRadius(), 0.0f);
        PR(MinTorsionalPatchRadius, o.getMinTorsionalPatchRadius(), 0.0f);

        Export(ele, o.getGeometry().any());
    }

    void ExportProperties(TiXmlNode& ele, PxRigidActor& o)
    {
        ExportProperty(ele, "Name", o.getName());

        PR(GlobalPose, o.getGlobalPose(), PxTransform());
        PFLAG(DisableGravity, o.getActorFlags(), PxActorFlag::eDISABLE_GRAVITY);
        PR(DominanceGroup, (uint32_t)o.getDominanceGroup(), 0);

        std::vector<PxShape*> shapes;
        shapes.resize(o.getNbShapes());
        o.getShapes(shapes.data(), (uint32_t)shapes.size(), 0);
        if (!shapes.empty()) {
            auto& shapesEle = *ele.InsertEndChild(TiXmlElement("Shapes"));
            for (auto shape : shapes) {
                Export(shapesEle, *shape);
            }
        }

        // These should be handled by the joint, not the rigidbody
        /*
        if (o.getNbConstraints() > 0) {
            s << "\t" "Constraints: ";
            PxConstraint* constraints[128];
            o.getConstraints(constraints, o.getNbConstraints(), 0);
            for (uint32_t i = 0; i < o.getNbConstraints(); i++) {
                s << constraints[i]->getConcreteTypeName() << " ";
            }
            s << std::endl;
        }*/
    }

    void Export(TiXmlNode& parent, PxRigidStatic& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("RigidStatic"));
        ExportProperties(ele, static_cast<PxRigidActor&>(o));
    }

    void ExportProperties(TiXmlNode& ele, PxRigidBody& o)
    {
        ExportProperties(ele, static_cast<PxRigidActor&>(o));

        PR(CMassLocalPose, o.getCMassLocalPose(), PxTransform());
        PR(Mass, o.getMass(), 1.0f);
        // PR(InvMass, o.getInvMass(), 1.0f); - Calculated by px
        PR(MassSpaceInertiaTensor, o.getMassSpaceInertiaTensor(), PxVec3(1.0f, 1.0f, 1.0f));
        // PR(MassSpaceInvInertiaTensor, o.getMassSpaceInvInertiaTensor(), PxVec3(1.0f, 1.0f, 1.0f)); - Calculated by px
        PR(LinearDamping, o.getLinearDamping(), 0.0f);
        PR(AngularDamping, o.getAngularDamping(), 0.05f);
        P_BOUNDED(MaxLinearVelocity, o.getMaxLinearVelocity(), 1e+15f); // 1e+16f
        PR(MaxAngularVelocity, o.getMaxAngularVelocity(), 100.0f);

        PFLAG(Kinematic, o.getRigidBodyFlags(), PxRigidBodyFlag::eKINEMATIC);
        PFLAG(EnableCCD, o.getRigidBodyFlags(), PxRigidBodyFlag::eENABLE_CCD);
        PFLAG(EnableCCDFriction, o.getRigidBodyFlags(), PxRigidBodyFlag::eENABLE_CCD_FRICTION);
        PFLAG(EnableSpeculativeCCD, o.getRigidBodyFlags(), PxRigidBodyFlag::eENABLE_SPECULATIVE_CCD);
        PFLAG(EnableCCDMaxContactImpulse, o.getRigidBodyFlags(), PxRigidBodyFlag::eENABLE_CCD_MAX_CONTACT_IMPULSE);
        PFLAG(RetainAccelerations, o.getRigidBodyFlags(), PxRigidBodyFlag::eRETAIN_ACCELERATIONS);

        PR(MinCCDAdvanceCoefficient, o.getMinCCDAdvanceCoefficient(), 0.15f);
        P_BOUNDED(MaxDepenetrationVelocity, o.getMaxDepenetrationVelocity(), 1e+31f); // 1e+32f
        P_BOUNDED(MaxContactImpulse, o.getMaxContactImpulse(), 1e+31f); // 1e+32f
    }

    void Export(TiXmlNode& parent, PxRigidDynamic& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("RigidDynamic"));
        ExportProperties(ele, static_cast<PxRigidBody&>(o));

        PxU32 minPositionIters, minVelocityIters;
        o.getSolverIterationCounts(minPositionIters, minVelocityIters);

        PR(SleepThreshold, o.getSleepThreshold(), 0.005f);
        PR(StabilizationThreshold, o.getStabilizationThreshold(), 0.0025f);

        PFLAG(LockLinearX, o.getRigidDynamicLockFlags(), PxRigidDynamicLockFlag::eLOCK_LINEAR_X);
        PFLAG(LockLinearY, o.getRigidDynamicLockFlags(), PxRigidDynamicLockFlag::eLOCK_LINEAR_Y);
        PFLAG(LockLinearZ, o.getRigidDynamicLockFlags(), PxRigidDynamicLockFlag::eLOCK_LINEAR_Z);
        PFLAG(LockAngularX, o.getRigidDynamicLockFlags(), PxRigidDynamicLockFlag::eLOCK_ANGULAR_X);
        PFLAG(LockAngularY, o.getRigidDynamicLockFlags(), PxRigidDynamicLockFlag::eLOCK_ANGULAR_Y);
        PFLAG(LockAngularZ, o.getRigidDynamicLockFlags(), PxRigidDynamicLockFlag::eLOCK_ANGULAR_Z);

        PR(WakeCounter, o.getWakeCounter(), 0);
        P_BOUNDED(ContactReportThreshold, o.getContactReportThreshold(), 3.40282e+37f); // PX_MAX_F32
        PR(MinPositionIters, minPositionIters, 4);
        PR(MinVelocityIters, minVelocityIters, 1);
    }

    void Export(TiXmlNode& parent, PxD6Joint& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("D6Joint"));
        ExportProperties(ele, static_cast<PxJoint&>(o));

        PR(MotionX, o.getMotion(PxD6Axis::eX), PxD6Motion::eFREE);
        PR(MotionY, o.getMotion(PxD6Axis::eY), PxD6Motion::eFREE);
        PR(MotionZ, o.getMotion(PxD6Axis::eZ), PxD6Motion::eFREE);
        PR(MotionTwist, o.getMotion(PxD6Axis::eTWIST), PxD6Motion::eFREE);
        PR(MotionSwing1, o.getMotion(PxD6Axis::eSWING1), PxD6Motion::eFREE);
        PR(MotionSwing2, o.getMotion(PxD6Axis::eSWING2), PxD6Motion::eFREE);

        P(DistanceLimit, o.getDistanceLimit());
        P(LinearLimitX, o.getLinearLimit(PxD6Axis::eX));
        P(LinearLimitY, o.getLinearLimit(PxD6Axis::eY));
        P(LinearLimitZ, o.getLinearLimit(PxD6Axis::eZ));
        P(TwistLimit, o.getTwistLimit());
        P(SwingLimit, o.getSwingLimit());
        P(PyramidSwingLimit, o.getPyramidSwingLimit());

        P(DriveX, o.getDrive(PxD6Drive::eX));
        P(DriveY, o.getDrive(PxD6Drive::eY));
        P(DriveZ, o.getDrive(PxD6Drive::eZ));
        P(DriveSwing, o.getDrive(PxD6Drive::eSWING));
        P(DriveTwist, o.getDrive(PxD6Drive::eTWIST));
        P(DriveSlerp, o.getDrive(PxD6Drive::eSLERP));

        PR(ProjectionLinearTolerance, o.getProjectionLinearTolerance(), 1e+10f);
        PR(ProjectionAngularTolerance, o.getProjectionAngularTolerance(), 3.14159f);
    }

    void ExportProperties(TiXmlNode& ele, PxJoint& o)
    {
        ExportProperty(ele, "Name", o.getName());

        PxRigidActor* actor0 = nullptr, * actor1 = nullptr;
        o.getActors(actor0, actor1);

        if (actor0) ExportProperty(ele, "Actor0", actor0->getName());
        if (actor1) ExportProperty(ele, "Actor1", actor1->getName());

        PR(Actor0LocalPose, o.getLocalPose(PxJointActorIndex::eACTOR0), PxTransform());
        PR(Actor1LocalPose, o.getLocalPose(PxJointActorIndex::eACTOR1), PxTransform());

        PxReal force, torque;
        o.getBreakForce(force, torque);
        P_BOUNDED(BreakForce, force, 3.40282e+37f); // PX_MAX_F32
        P_BOUNDED(BreakTorque, torque, 3.40282e+37f); // PX_MAX_F32

        PFLAG(ProjectToActor0, o.getConstraintFlags(), PxConstraintFlag::ePROJECT_TO_ACTOR0);
        PFLAG(ProjectToActor1, o.getConstraintFlags(), PxConstraintFlag::ePROJECT_TO_ACTOR1);
        PFLAG(CollisionEnabled, o.getConstraintFlags(), PxConstraintFlag::eCOLLISION_ENABLED);
        PFLAG(DriveLimitsAreForces, o.getConstraintFlags(), PxConstraintFlag::eDRIVE_LIMITS_ARE_FORCES);

        PR(InvMassScale0, o.getInvMassScale0(), 1.0f);
        PR(InvInertiaScale0, o.getInvInertiaScale0(), 1.0f);
        PR(InvMassScale1, o.getInvMassScale1(), 1.0f);
        PR(InvInertiaScale1, o.getInvInertiaScale1(), 1.0f);
    }

    void Export(TiXmlNode& parent, PxArticulationJoint& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("Joint"));
        ExportProperties(ele, static_cast<PxArticulationJointBase&>(o));

        PR(Stiffness, o.getStiffness(), 0.0f);
        PR(Damping, o.getDamping(), 0.0f);
        PR(InternalCompliance, o.getInternalCompliance(), 0.0f);
        PR(ExternalCompliance, o.getExternalCompliance(), 0.0f);

        PxReal zLimit, yLimit;
        o.getSwingLimit(zLimit, yLimit);
        PR(SwingLimitZ, zLimit, (float)M_PI / 4);
        PR(SwingLimitY, yLimit, (float)M_PI / 4);

        PR(TangentialStiffness, o.getTangentialStiffness(), 0.0f);
        PR(TangentialDamping, o.getTangentialDamping(), 0.0f);
        PR(SwingLimitContactDistance, o.getSwingLimitContactDistance(), 0.05f);
        PR(SwingLimitEnabled, o.getSwingLimitEnabled(), false);

        PxReal lower, upper;
        o.getTwistLimit(lower, upper);
        PR(TwistLimitLower, lower, -(float)M_PI / 4);
        PR(TwistLimitUpper, upper, (float)M_PI / 4);

        PR(TwistLimitContactDistance, o.getTwistLimitContactDistance(), 0.05f);
        PR(TwistLimitEnabled, o.getTwistLimitEnabled(), false);
    }

    void ExportProperties(TiXmlNode& ele, PxArticulationJointBase& o)
    {
        PR(ParentPose, o.getParentPose(), PxTransform());
        PR(ChildPose, o.getChildPose(), PxTransform());
    }

    void Export(TiXmlNode& parent, PxArticulationLink& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("Link"));
        ExportProperties(ele, static_cast<PxRigidBody&>(o));

        auto joint = o.getInboundJoint();
        if (joint != nullptr) {
            PR(InboundJointDof, o.getInboundJointDof(), 0);
            Export(ele, static_cast<PxArticulationJoint&>(*joint));
        }

        if (o.getNbChildren() > 0) {
            std::vector<PxArticulationLink*> children;
            children.resize(o.getNbChildren());
            o.getChildren(children.data(), (PxU32)children.size(), 0);

            auto& childrenEle = *ele.InsertEndChild(TiXmlElement("Links"));

            for (auto child : children) {
                Export(childrenEle, *child);
            }
        }
    }

    void Export(TiXmlNode& parent, PxArticulation& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("Articulation"));
        ExportProperties(ele, static_cast<PxArticulationBase&>(o));

        PR(MaxProjectionIterations, o.getMaxProjectionIterations(), 4);
        PR(SeparationTolerance, o.getSeparationTolerance(), 0.01f);
        PR(InternalDriveIterations, o.getInternalDriveIterations(), 4);
        PR(ExternalDriveIterations, o.getExternalDriveIterations(), 4);
    }

    void ExportProperties(TiXmlNode& ele, PxArticulationBase& o)
    {
        PR(SleepThreshold, o.getSleepThreshold(), 0.005f);
        PR(StabilizationThreshold, o.getStabilizationThreshold(), 0.0025f);
        PR(WakeCounter, o.getWakeCounter(), 0.4f);

        if (o.getNbLinks() > 0) {
            std::vector<PxArticulationLink*> children;
            children.resize(o.getNbLinks());
            o.getLinks(children.data(), (PxU32)children.size(), 0);

            auto& childrenEle = *ele.InsertEndChild(TiXmlElement("Links"));

            for (auto child : children) {
                if (child->getInboundJoint() == nullptr) {
                    Export(childrenEle, *child);
                }
            }
        }
    }

    void Export(TiXmlNode& parent, PxConvexMesh& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("ConvexMesh"));

        auto verts = o.getVertices();
        auto inds = o.getIndexBuffer();
        auto polys = o.getNbPolygons();

        for (PxU32 i = 0; i < polys; i++) {
            PxHullPolygon poly;
            o.getPolygonData(i, poly);

            auto& polyEle = *ele.InsertEndChild(TiXmlElement("Polygon"));

            for (PxU32 v = 0; v < poly.mNbVerts; v++) {
                auto vert = verts[inds[poly.mIndexBase + v]];
                ExportProperty(polyEle, "Vertex", vert);
            }
        }
    }

    void Export(TiXmlNode& parent, PxTriangleMesh& o)
    {
        auto& ele = *parent.InsertEndChild(TiXmlElement("TriangleMesh"));

        auto verts = o.getVertices();
        auto inds = (PxU16*)o.getTriangles();
        auto tris = o.getNbTriangles();

        for (PxU32 i = 0; i < tris*3; i++) {
            auto vert = verts[inds[i]];
            ExportProperty(ele, "Vertex", vert);
        }
    }

    void ExportTopLevel(TiXmlNode& parent, PxBase& obj)
    {
        switch (obj.getConcreteType()) {
        case PxTypeInfo<PxMaterial>::eFastTypeId: return Export(parent, static_cast<PxMaterial&>(obj));
        case PxTypeInfo<PxRigidStatic>::eFastTypeId: Export(parent, static_cast<PxRigidStatic&>(obj)); break;
        case PxTypeInfo<PxRigidDynamic>::eFastTypeId: Export(parent, static_cast<PxRigidDynamic&>(obj)); break;
        case PxTypeInfo<PxD6Joint>::eFastTypeId: Export(parent, static_cast<PxD6Joint&>(obj)); break;
        case PxTypeInfo<PxArticulation>::eFastTypeId: Export(parent, static_cast<PxArticulation&>(obj)); break;

        // These are child nodes of other types and will be exported alongside them
        case PxTypeInfo<PxShape>::eFastTypeId:
        case PxTypeInfo<PxConstraint>::eFastTypeId:
        case PxTypeInfo<PxArticulationJoint>::eFastTypeId:
        case PxTypeInfo<PxArticulationLink>::eFastTypeId:
        case PxTypeInfo<PxConvexMesh>::eFastTypeId:
        case PxTypeInfo<PxBVH33TriangleMesh>::eFastTypeId:
            return;

        default:
            std::cout << "WARNING: Unknown element in PxCollection: " << obj.getConcreteTypeName() << std::endl;
            return;
        }
    }
};

std::vector<uint8_t> PhysXConverter::SaveCollectionToXml(PxCollection& collection)
{
    PhysXExporter exporter;
    auto xml = exporter.Export(collection);
    TiXmlPrinter printer;
    xml->Accept(&printer);
    return std::vector<uint8_t>((uint8_t const*)printer.Str().data(), (uint8_t const*)printer.Str().data() + printer.Str().size());;
}
