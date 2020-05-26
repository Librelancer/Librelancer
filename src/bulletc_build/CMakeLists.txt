CMAKE_MINIMUM_REQUIRED (VERSION 2.8.9)
PROJECT (libbulletc)

IF (NOT CMAKE_BUILD_TYPE)
    # SET(CMAKE_BUILD_TYPE "Debug")
    SET(CMAKE_BUILD_TYPE "Release")
ENDIF (NOT CMAKE_BUILD_TYPE)

FIND_PATH(BULLET_INCLUDE_DIR NAMES btBulletCollisionCommon.h
    PATHS
    ${PROJECT_SOURCE_DIR}/../../extern/bullet3/src/
    ENV CPATH
    /usr/include
    /usr/local/include
    /opt/local/include
    NO_DEFAULT_PATH
)

set(BUILD_EXTRAS OFF CACHE BOOL "")
set(BUILD_BULLET2_DEMOS OFF CACHE BOOL "")
set(BUILD_BULLET3 OFF CACHE BOOL "")
set(BUILD_CPU_DEMOS OFF CACHE BOOL "")
set(BUILD_UNIT_TESTS OFF CACHE BOOL "")
set(CSRCDIR "${PROJECT_SOURCE_DIR}/../../extern/BulletSharpPInvoke/libbulletc/src")

add_definitions(-DBULLETC_DISABLE_HACD)

SET(CMAKE_POSITION_INDEPENDENT_CODE ON)

ADD_SUBDIRECTORY("${BULLET_INCLUDE_DIR}/.." "${CMAKE_CURRENT_BINARY_DIR}/bullet")

INCLUDE_DIRECTORIES(${BULLET_INCLUDE_DIR})
INCLUDE_DIRECTORIES(${BULLET_PHYSICS_SOURCE_DIR}/Extras/HACD/)
INCLUDE_DIRECTORIES(${BULLET_PHYSICS_SOURCE_DIR}/Extras/Serialize/BulletWorldImporter/)
INCLUDE_DIRECTORIES(${BULLET_PHYSICS_SOURCE_DIR}/Extras/Serialize/BulletXmlWorldImporter/)

IF(${CMAKE_GENERATOR} MATCHES "Unix Makefiles")
    SET(BULLETC_LIB bulletc)
ELSE()
    SET(BULLETC_LIB libbulletc)
ENDIF()

IF (USE_DOUBLE_PRECISION)
    ADD_DEFINITIONS( -DBT_USE_DOUBLE_PRECISION)
ENDIF (USE_DOUBLE_PRECISION)

IF(MSVC)
    IF (NOT USE_MSVC_RUNTIME_LIBRARY_DLL)
        #We statically link to reduce dependancies
        FOREACH(flag_var CMAKE_CXX_FLAGS CMAKE_CXX_FLAGS_DEBUG CMAKE_CXX_FLAGS_RELEASE CMAKE_CXX_FLAGS_MINSIZEREL CMAKE_CXX_FLAGS_RELWITHDEBINFO CMAKE_C_FLAGS CMAKE_C_FLAGS_DEBUG CMAKE_C_FLAGS_RELEASE CMAKE_C_FLAGS_MINSIZEREL CMAKE_C_FLAGS_RELWITHDEBINFO )
            IF(${flag_var} MATCHES "/MD")
                STRING(REGEX REPLACE "/MD" "/MT" ${flag_var} "${${flag_var}}")
            ENDIF(${flag_var} MATCHES "/MD")
            IF(${flag_var} MATCHES "/MDd")
                STRING(REGEX REPLACE "/MDd" "/MTd" ${flag_var} "${${flag_var}}")
            ENDIF(${flag_var} MATCHES "/MDd")
        ENDFOREACH(flag_var)
    ENDIF()

    IF (CMAKE_CL_64)
        ADD_DEFINITIONS(-D_WIN64)
    ELSE()
        IF (USE_MSVC_SSE)
            ADD_DEFINITIONS(/arch:SSE)
        ENDIF()
        IF (USE_MSVC_SSE2)
            ADD_DEFINITIONS(/arch:SSE2)
        ENDIF()
    ENDIF()

    IF (USE_MSVC_AVX)
        ADD_DEFINITIONS(/arch:AVX)
    ENDIF()

    IF (USE_MSVC_FAST_FLOATINGPOINT)
        ADD_DEFINITIONS(/fp:fast)
    ENDIF()
ENDIF(MSVC)

ADD_LIBRARY(${BULLETC_LIB} SHARED
    ${CSRCDIR}/../dllmain.cpp
    ${CSRCDIR}/conversion.h
    ${CSRCDIR}/main.h
    ${CSRCDIR}/collections.cpp
    ${CSRCDIR}/collections.h
    ${CSRCDIR}/btActionInterface_wrap.cpp
    ${CSRCDIR}/btActionInterface_wrap.h
    ${CSRCDIR}/btAlignedObjectArray_wrap.cpp
    ${CSRCDIR}/btAlignedObjectArray_wrap.h
    ${CSRCDIR}/btAxisSweep3_wrap.cpp
    ${CSRCDIR}/btAxisSweep3_wrap.h
    ${CSRCDIR}/btBox2dBox2dCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btBox2dBox2dCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btBox2dShape_wrap.cpp
    ${CSRCDIR}/btBox2dShape_wrap.h
    ${CSRCDIR}/btBoxBoxCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btBoxBoxCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btBoxBoxDetector_wrap.cpp
    ${CSRCDIR}/btBoxBoxDetector_wrap.h
    ${CSRCDIR}/btBoxCollision_wrap.cpp
    ${CSRCDIR}/btBoxCollision_wrap.h
    ${CSRCDIR}/btBoxShape_wrap.cpp
    ${CSRCDIR}/btBoxShape_wrap.h
    ${CSRCDIR}/btBroadphaseInterface_wrap.cpp
    ${CSRCDIR}/btBroadphaseInterface_wrap.h
    ${CSRCDIR}/btBroadphaseProxy_wrap.cpp
    ${CSRCDIR}/btBroadphaseProxy_wrap.h
    ${CSRCDIR}/btBulletFile_wrap.cpp
    ${CSRCDIR}/btBulletFile_wrap.h
    ${CSRCDIR}/btBulletWorldImporter_wrap.cpp
    ${CSRCDIR}/btBulletWorldImporter_wrap.h
    ${CSRCDIR}/btBulletXmlWorldImporter_wrap.cpp
    ${CSRCDIR}/btBulletXmlWorldImporter_wrap.h
    ${CSRCDIR}/btBvhTriangleMeshShape_wrap.cpp
    ${CSRCDIR}/btBvhTriangleMeshShape_wrap.h
    ${CSRCDIR}/btCapsuleShape_wrap.cpp
    ${CSRCDIR}/btCapsuleShape_wrap.h
    ${CSRCDIR}/btCharacterControllerInterface_wrap.cpp
    ${CSRCDIR}/btCharacterControllerInterface_wrap.h
    ${CSRCDIR}/btCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btCollisionConfiguration_wrap.cpp
    ${CSRCDIR}/btCollisionConfiguration_wrap.h
    ${CSRCDIR}/btCollisionCreateFunc_wrap.cpp
    ${CSRCDIR}/btCollisionCreateFunc_wrap.h
    ${CSRCDIR}/btCollisionDispatcher_wrap.cpp
    ${CSRCDIR}/btCollisionDispatcher_wrap.h
    ${CSRCDIR}/btCollisionDispatcherMt_wrap.cpp
    ${CSRCDIR}/btCollisionDispatcherMt_wrap.h
    ${CSRCDIR}/btCollisionObject_wrap.cpp
    ${CSRCDIR}/btCollisionObject_wrap.h
    ${CSRCDIR}/btCollisionObjectWrapper_wrap.cpp
    ${CSRCDIR}/btCollisionObjectWrapper_wrap.h
    ${CSRCDIR}/btCollisionShape_wrap.cpp
    ${CSRCDIR}/btCollisionShape_wrap.h
    ${CSRCDIR}/btCollisionWorld_wrap.cpp
    ${CSRCDIR}/btCollisionWorld_wrap.h
    ${CSRCDIR}/btCompoundCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btCompoundCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btCompoundCompoundCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btCompoundCompoundCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btCompoundFromGimpact_wrap.cpp
    ${CSRCDIR}/btCompoundFromGimpact_wrap.h
    ${CSRCDIR}/btCompoundShape_wrap.cpp
    ${CSRCDIR}/btCompoundShape_wrap.h
    ${CSRCDIR}/btConcaveShape_wrap.cpp
    ${CSRCDIR}/btConcaveShape_wrap.h
    ${CSRCDIR}/btConeShape_wrap.cpp
    ${CSRCDIR}/btConeShape_wrap.h
    ${CSRCDIR}/btConeTwistConstraint_wrap.cpp
    ${CSRCDIR}/btConeTwistConstraint_wrap.h
    ${CSRCDIR}/btConstraintSolver_wrap.cpp
    ${CSRCDIR}/btConstraintSolver_wrap.h
    ${CSRCDIR}/btContactConstraint_wrap.cpp
    ${CSRCDIR}/btContactConstraint_wrap.h
    ${CSRCDIR}/btContactSolverInfo_wrap.cpp
    ${CSRCDIR}/btContactSolverInfo_wrap.h
    ${CSRCDIR}/btContinuousConvexCollision_wrap.cpp
    ${CSRCDIR}/btContinuousConvexCollision_wrap.h
    ${CSRCDIR}/btConvex2dConvex2dAlgorithm_wrap.cpp
    ${CSRCDIR}/btConvex2dConvex2dAlgorithm_wrap.h
    ${CSRCDIR}/btConvex2dShape_wrap.cpp
    ${CSRCDIR}/btConvex2dShape_wrap.h
    ${CSRCDIR}/btConvexCast_wrap.cpp
    ${CSRCDIR}/btConvexCast_wrap.h
    ${CSRCDIR}/btConvexConcaveCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btConvexConcaveCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btConvexConvexAlgorithm_wrap.cpp
    ${CSRCDIR}/btConvexConvexAlgorithm_wrap.h
    ${CSRCDIR}/btConvexHullShape_wrap.cpp
    ${CSRCDIR}/btConvexHullShape_wrap.h
    ${CSRCDIR}/btConvexInternalShape_wrap.cpp
    ${CSRCDIR}/btConvexInternalShape_wrap.h
    ${CSRCDIR}/btConvexPenetrationDepthSolver_wrap.cpp
    ${CSRCDIR}/btConvexPenetrationDepthSolver_wrap.h
    ${CSRCDIR}/btConvexPlaneCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btConvexPlaneCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btConvexPointCloudShape_wrap.cpp
    ${CSRCDIR}/btConvexPointCloudShape_wrap.h
    ${CSRCDIR}/btConvexPolyhedron_wrap.cpp
    ${CSRCDIR}/btConvexPolyhedron_wrap.h
    ${CSRCDIR}/btConvexShape_wrap.cpp
    ${CSRCDIR}/btConvexShape_wrap.h
    ${CSRCDIR}/btConvexTriangleMeshShape_wrap.cpp
    ${CSRCDIR}/btConvexTriangleMeshShape_wrap.h
    ${CSRCDIR}/btCpuFeatureUtility_wrap.cpp
    ${CSRCDIR}/btCpuFeatureUtility_wrap.h
    ${CSRCDIR}/btCylinderShape_wrap.cpp
    ${CSRCDIR}/btCylinderShape_wrap.h
    ${CSRCDIR}/btDantzigSolver_wrap.cpp
    ${CSRCDIR}/btDantzigSolver_wrap.h
    ${CSRCDIR}/btDbvt_wrap.cpp
    ${CSRCDIR}/btDbvt_wrap.h
    ${CSRCDIR}/btDbvtBroadphase_wrap.cpp
    ${CSRCDIR}/btDbvtBroadphase_wrap.h
    ${CSRCDIR}/btDefaultCollisionConfiguration_wrap.cpp
    ${CSRCDIR}/btDefaultCollisionConfiguration_wrap.h
    ${CSRCDIR}/btDefaultMotionState_wrap.cpp
    ${CSRCDIR}/btDefaultMotionState_wrap.h
    ${CSRCDIR}/btDefaultSoftBodySolver_wrap.cpp
    ${CSRCDIR}/btDefaultSoftBodySolver_wrap.h
    ${CSRCDIR}/btDiscreteCollisionDetectorInterface_wrap.cpp
    ${CSRCDIR}/btDiscreteCollisionDetectorInterface_wrap.h
    ${CSRCDIR}/btDiscreteDynamicsWorld_wrap.cpp
    ${CSRCDIR}/btDiscreteDynamicsWorld_wrap.h
    ${CSRCDIR}/btDiscreteDynamicsWorldMt_wrap.cpp
    ${CSRCDIR}/btDiscreteDynamicsWorldMt_wrap.h
    ${CSRCDIR}/btDispatcher_wrap.cpp
    ${CSRCDIR}/btDispatcher_wrap.h
    ${CSRCDIR}/btDynamicsWorld_wrap.cpp
    ${CSRCDIR}/btDynamicsWorld_wrap.h
    ${CSRCDIR}/btEmptyCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btEmptyCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btEmptyShape_wrap.cpp
    ${CSRCDIR}/btEmptyShape_wrap.h
    ${CSRCDIR}/btFixedConstraint_wrap.cpp
    ${CSRCDIR}/btFixedConstraint_wrap.h
    ${CSRCDIR}/btGearConstraint_wrap.cpp
    ${CSRCDIR}/btGearConstraint_wrap.h
    ${CSRCDIR}/btGeneric6DofConstraint_wrap.cpp
    ${CSRCDIR}/btGeneric6DofConstraint_wrap.h
    ${CSRCDIR}/btGeneric6DofSpring2Constraint_wrap.cpp
    ${CSRCDIR}/btGeneric6DofSpring2Constraint_wrap.h
    ${CSRCDIR}/btGeneric6DofSpringConstraint_wrap.cpp
    ${CSRCDIR}/btGeneric6DofSpringConstraint_wrap.h
    ${CSRCDIR}/btGeometryUtil_wrap.cpp
    ${CSRCDIR}/btGeometryUtil_wrap.h
    ${CSRCDIR}/btGhostObject_wrap.cpp
    ${CSRCDIR}/btGhostObject_wrap.h
    ${CSRCDIR}/btGImpactBvh_wrap.cpp
    ${CSRCDIR}/btGImpactBvh_wrap.h
    ${CSRCDIR}/btGImpactCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btGImpactCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btGImpactQuantizedBvh_wrap.cpp
    ${CSRCDIR}/btGImpactQuantizedBvh_wrap.h
    ${CSRCDIR}/btGImpactShape_wrap.cpp
    ${CSRCDIR}/btGImpactShape_wrap.h
    ${CSRCDIR}/btGjkConvexCast_wrap.cpp
    ${CSRCDIR}/btGjkConvexCast_wrap.h
    ${CSRCDIR}/btGjkEpaPenetrationDepthSolver_wrap.cpp
    ${CSRCDIR}/btGjkEpaPenetrationDepthSolver_wrap.h
    ${CSRCDIR}/btGjkPairDetector_wrap.cpp
    ${CSRCDIR}/btGjkPairDetector_wrap.h
    ${CSRCDIR}/btHeightfieldTerrainShape_wrap.cpp
    ${CSRCDIR}/btHeightfieldTerrainShape_wrap.h
    ${CSRCDIR}/btHinge2Constraint_wrap.cpp
    ${CSRCDIR}/btHinge2Constraint_wrap.h
    ${CSRCDIR}/btHingeConstraint_wrap.cpp
    ${CSRCDIR}/btHingeConstraint_wrap.h
    ${CSRCDIR}/btIDebugDraw_wrap.cpp
    ${CSRCDIR}/btIDebugDraw_wrap.h
    ${CSRCDIR}/btKinematicCharacterController_wrap.cpp
    ${CSRCDIR}/btKinematicCharacterController_wrap.h
    ${CSRCDIR}/btLemkeSolver_wrap.cpp
    ${CSRCDIR}/btLemkeSolver_wrap.h
    ${CSRCDIR}/btManifoldPoint_wrap.cpp
    ${CSRCDIR}/btManifoldPoint_wrap.h
    ${CSRCDIR}/btManifoldResult_wrap.cpp
    ${CSRCDIR}/btManifoldResult_wrap.h
    ${CSRCDIR}/btMinkowskiPenetrationDepthSolver_wrap.cpp
    ${CSRCDIR}/btMinkowskiPenetrationDepthSolver_wrap.h
    ${CSRCDIR}/btMinkowskiSumShape_wrap.cpp
    ${CSRCDIR}/btMinkowskiSumShape_wrap.h
    ${CSRCDIR}/btMLCPSolver_wrap.cpp
    ${CSRCDIR}/btMLCPSolver_wrap.h
    ${CSRCDIR}/btMLCPSolverInterface_wrap.cpp
    ${CSRCDIR}/btMLCPSolverInterface_wrap.h
    ${CSRCDIR}/btMotionState_wrap.cpp
    ${CSRCDIR}/btMotionState_wrap.h
    ${CSRCDIR}/btMultiBody_wrap.cpp
    ${CSRCDIR}/btMultiBody_wrap.h
    ${CSRCDIR}/btMultiBodyConstraint_wrap.cpp
    ${CSRCDIR}/btMultiBodyConstraint_wrap.h
    ${CSRCDIR}/btMultiBodyConstraintSolver_wrap.cpp
    ${CSRCDIR}/btMultiBodyConstraintSolver_wrap.h
    ${CSRCDIR}/btMultiBodyDynamicsWorld_wrap.cpp
    ${CSRCDIR}/btMultiBodyDynamicsWorld_wrap.h
    ${CSRCDIR}/btMultiBodyFixedConstraint_wrap.cpp
    ${CSRCDIR}/btMultiBodyFixedConstraint_wrap.h
    ${CSRCDIR}/btMultiBodyJointLimitConstraint_wrap.cpp
    ${CSRCDIR}/btMultiBodyJointLimitConstraint_wrap.h
    ${CSRCDIR}/btMultiBodyJointMotor_wrap.cpp
    ${CSRCDIR}/btMultiBodyJointMotor_wrap.h
    ${CSRCDIR}/btMultiBodyLink_wrap.cpp
    ${CSRCDIR}/btMultiBodyLink_wrap.h
    ${CSRCDIR}/btMultiBodyLinkCollider_wrap.cpp
    ${CSRCDIR}/btMultiBodyLinkCollider_wrap.h
    ${CSRCDIR}/btMultiBodyPoint2Point_wrap.cpp
    ${CSRCDIR}/btMultiBodyPoint2Point_wrap.h
    ${CSRCDIR}/btMultiBodySliderConstraint_wrap.cpp
    ${CSRCDIR}/btMultiBodySliderConstraint_wrap.h
    ${CSRCDIR}/btMultiBodySolverConstraint_wrap.cpp
    ${CSRCDIR}/btMultiBodySolverConstraint_wrap.h
    ${CSRCDIR}/btMultimaterialTriangleMeshShape_wrap.cpp
    ${CSRCDIR}/btMultimaterialTriangleMeshShape_wrap.h
    ${CSRCDIR}/btMultiSphereShape_wrap.cpp
    ${CSRCDIR}/btMultiSphereShape_wrap.h
    ${CSRCDIR}/btNNCGConstraintSolver_wrap.cpp
    ${CSRCDIR}/btNNCGConstraintSolver_wrap.h
    ${CSRCDIR}/btOptimizedBvh_wrap.cpp
    ${CSRCDIR}/btOptimizedBvh_wrap.h
    ${CSRCDIR}/btOverlappingPairCache_wrap.cpp
    ${CSRCDIR}/btOverlappingPairCache_wrap.h
    ${CSRCDIR}/btOverlappingPairCallback_wrap.cpp
    ${CSRCDIR}/btOverlappingPairCallback_wrap.h
    ${CSRCDIR}/btPersistentManifold_wrap.cpp
    ${CSRCDIR}/btPersistentManifold_wrap.h
    ${CSRCDIR}/btPoint2PointConstraint_wrap.cpp
    ${CSRCDIR}/btPoint2PointConstraint_wrap.h
    ${CSRCDIR}/btPointCollector_wrap.cpp
    ${CSRCDIR}/btPointCollector_wrap.h
    ${CSRCDIR}/btPolarDecomposition_wrap.cpp
    ${CSRCDIR}/btPolarDecomposition_wrap.h
    ${CSRCDIR}/btPolyhedralConvexShape_wrap.cpp
    ${CSRCDIR}/btPolyhedralConvexShape_wrap.h
    ${CSRCDIR}/btQuantizedBvh_wrap.cpp
    ${CSRCDIR}/btQuantizedBvh_wrap.h
    ${CSRCDIR}/btRaycastVehicle_wrap.cpp
    ${CSRCDIR}/btRaycastVehicle_wrap.h
    ${CSRCDIR}/btRigidBody_wrap.cpp
    ${CSRCDIR}/btRigidBody_wrap.h
    ${CSRCDIR}/btScaledBvhTriangleMeshShape_wrap.cpp
    ${CSRCDIR}/btScaledBvhTriangleMeshShape_wrap.h
    ${CSRCDIR}/btSequentialImpulseConstraintSolver_wrap.cpp
    ${CSRCDIR}/btSequentialImpulseConstraintSolver_wrap.h
    ${CSRCDIR}/btSerializer_wrap.cpp
    ${CSRCDIR}/btSerializer_wrap.h
    ${CSRCDIR}/btShapeHull_wrap.cpp
    ${CSRCDIR}/btShapeHull_wrap.h
    ${CSRCDIR}/btSimulationIslandManager_wrap.cpp
    ${CSRCDIR}/btSimulationIslandManager_wrap.h
    ${CSRCDIR}/btSliderConstraint_wrap.cpp
    ${CSRCDIR}/btSliderConstraint_wrap.h
    ${CSRCDIR}/btSoftBody_wrap.cpp
    ${CSRCDIR}/btSoftBody_wrap.h
    ${CSRCDIR}/btSoftBodyConcaveCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btSoftBodyConcaveCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btSoftBodyHelpers_wrap.cpp
    ${CSRCDIR}/btSoftBodyHelpers_wrap.h
    ${CSRCDIR}/btSoftBodyRigidBodyCollisionConfiguration_wrap.cpp
    ${CSRCDIR}/btSoftBodyRigidBodyCollisionConfiguration_wrap.h
    ${CSRCDIR}/btSoftBodySolvers_wrap.cpp
    ${CSRCDIR}/btSoftBodySolvers_wrap.h
    ${CSRCDIR}/btSoftRigidCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btSoftRigidCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btSoftRigidDynamicsWorld_wrap.cpp
    ${CSRCDIR}/btSoftRigidDynamicsWorld_wrap.h
    ${CSRCDIR}/btSoftSoftCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btSoftSoftCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btSparseSdf3_wrap.cpp
    ${CSRCDIR}/btSparseSdf3_wrap.h
    ${CSRCDIR}/btSphereBoxCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btSphereBoxCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btSphereShape_wrap.cpp
    ${CSRCDIR}/btSphereShape_wrap.h
    ${CSRCDIR}/btSphereSphereCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btSphereSphereCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btSphereTriangleCollisionAlgorithm_wrap.cpp
    ${CSRCDIR}/btSphereTriangleCollisionAlgorithm_wrap.h
    ${CSRCDIR}/btStaticPlaneShape_wrap.cpp
    ${CSRCDIR}/btStaticPlaneShape_wrap.h
    ${CSRCDIR}/btStridingMeshInterface_wrap.cpp
    ${CSRCDIR}/btStridingMeshInterface_wrap.h
    ${CSRCDIR}/btTetrahedronShape_wrap.cpp
    ${CSRCDIR}/btTetrahedronShape_wrap.h
    ${CSRCDIR}/btThreads_wrap.cpp
    ${CSRCDIR}/btThreads_wrap.h
    ${CSRCDIR}/btTransformUtil_wrap.cpp
    ${CSRCDIR}/btTransformUtil_wrap.h
    ${CSRCDIR}/btTriangleBuffer_wrap.cpp
    ${CSRCDIR}/btTriangleBuffer_wrap.h
    ${CSRCDIR}/btTriangleCallback_wrap.cpp
    ${CSRCDIR}/btTriangleCallback_wrap.h
    ${CSRCDIR}/btTriangleIndexVertexArray_wrap.cpp
    ${CSRCDIR}/btTriangleIndexVertexArray_wrap.h
    ${CSRCDIR}/btTriangleIndexVertexMaterialArray_wrap.cpp
    ${CSRCDIR}/btTriangleIndexVertexMaterialArray_wrap.h
    ${CSRCDIR}/btTriangleMesh_wrap.cpp
    ${CSRCDIR}/btTriangleMesh_wrap.h
    ${CSRCDIR}/btTriangleMeshShape_wrap.cpp
    ${CSRCDIR}/btTriangleMeshShape_wrap.h
    ${CSRCDIR}/btTriangleShape_wrap.cpp
    ${CSRCDIR}/btTriangleShape_wrap.h
    ${CSRCDIR}/btTriangleShapeEx_wrap.cpp
    ${CSRCDIR}/btTriangleShapeEx_wrap.h
    ${CSRCDIR}/btTypedConstraint_wrap.cpp
    ${CSRCDIR}/btTypedConstraint_wrap.h
    ${CSRCDIR}/btUniformScalingShape_wrap.cpp
    ${CSRCDIR}/btUniformScalingShape_wrap.h
    ${CSRCDIR}/btUnionFind_wrap.cpp
    ${CSRCDIR}/btUnionFind_wrap.h
    ${CSRCDIR}/btUniversalConstraint_wrap.cpp
    ${CSRCDIR}/btUniversalConstraint_wrap.h
    ${CSRCDIR}/btVehicleRaycaster_wrap.cpp
    ${CSRCDIR}/btVehicleRaycaster_wrap.h
    ${CSRCDIR}/btVoronoiSimplexSolver_wrap.cpp
    ${CSRCDIR}/btVoronoiSimplexSolver_wrap.h
    ${CSRCDIR}/btWheelInfo_wrap.cpp
    ${CSRCDIR}/btWheelInfo_wrap.h
    ${CSRCDIR}/btWorldImporter_wrap.cpp
    ${CSRCDIR}/btWorldImporter_wrap.h
    ${CSRCDIR}/hacdHACD_wrap.cpp
    ${CSRCDIR}/hacdHACD_wrap.h
)

TARGET_LINK_LIBRARIES(${BULLETC_LIB} BulletCollision BulletDynamics LinearMath BulletSoftBody)
