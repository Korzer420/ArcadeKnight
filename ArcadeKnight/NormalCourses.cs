using ArcadeKnight.Enums;
using System.Collections.Generic;

namespace ArcadeKnight;

public static class NormalCourses
{
    public static List<CourseMetaData> GorbCourses = 
    [
        new()
                {
                    Name = "Cliffhanger",
                    EasyCourse = new()
                    {
                        StartPositionX = 4.0938f,
                        StartPositionY = 24.4081f,
                        EndPositionX = 222.89f,
                        EndPositionY = 28.4f,
                        ObjectsToRemove =
                        [
                            "plat_float_01",
                            "plat_float_01 (1)",
                            "plat_float_02",
                            "plat_float_03",
                            "plat_float_05"
                        ],
                        PreviewPoints =
                        [
                            new(49.58f, 9.6f),
                            new(86.45f, 9.6f),
                            new(115.78f, 32.76f),
                            new(137.54f, 30.9f),
                            new(161.9f, 22.24f),
                            new(174.03f, 28.95f)
                        ],
                        Obstacles =
                        [
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 56.66f,
                                YPosition = 6.4f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 116.04f,
                                YPosition = 31.4f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 35.52f,
                                YPosition = 11.4f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 89.64f,
                                YPosition = 6.4f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 129.59f,
                                YPosition = 32.86f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Platform",
                                XPosition = 124.49f,
                                YPosition = 36f,
                                Rotation = 270f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Platform",
                                XPosition = 224.49f,
                                YPosition = 35.3f,
                                Rotation = 90f
                            },
                            new AbilityModifier()
                            {
                                XPosition = 121.2908f,
                                YPosition = 32.5081f,
                                SetValue = true,
                                AffectedAbility = "hasSuperDash",
                                RevertDirection = CheckDirection.Left
                            }
                        ]
                    },
                    NormalCourse = new()
                    {
                        StartPositionX = 4.0938f,
                        StartPositionY = 24.4081f,
                        EndPositionX = 222.89f,
                        EndPositionY = 28.4f,
                        ObjectsToRemove = [],
                        PreviewPoints =
                        [
                            new(49.58f, 9.6f),
                            new(86.45f, 9.6f),
                            new(115.78f, 32.76f),
                            new(137.54f, 30.9f),
                            new(161.9f, 22.24f),
                            new(174.03f, 28.95f)
                        ],
                        Obstacles =
                        [
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 56.66f,
                                YPosition = 6.4f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 116.04f,
                                YPosition = 31.4f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 199.25f,
                                YPosition = 28.41f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 178.02f,
                                YPosition = 23.41f
                            },
                            new CourseObstacle()
                            {
                                ObjectName = "Wingmould",
                                XPosition = 215.43f,
                                YPosition = 28.41f
                            },
                            new AbilityModifier()
                            {
                                XPosition = 6.77f,
                                YPosition = 25.4f,
                                SetValue = false,
                                AffectedAbility = "hasSuperDash",
                                RevertDirection = CheckDirection.Left
                            }
                        ]
                    },
                    HardCourse = new()
                    {
                        StartPositionX = 4.0938f,
                        StartPositionY = 24.4081f,
                        EndPositionX = 222.89f,
                        EndPositionY = 28.4f,
                        ObjectsToRemove = [],
                        PreviewPoints =
                        [
                            new(49.58f, 9.6f),
                            new(86.45f, 9.6f),
                            new(115.78f, 32.76f),
                            new(137.54f, 30.9f),
                            new(161.9f, 22.24f),
                            new(174.03f, 28.95f)
                        ],
                        Obstacles =
                        [
                            new AbilityModifier()
                            {
                                XPosition = 6.77f,
                                YPosition = 25.4f,
                                SetValue = false,
                                AffectedAbility = "hasSuperDash",
                                RevertDirection = CheckDirection.Left
                            },
                            new AbilityModifier()
                            {
                                XPosition = 11.06f,
                                YPosition = 22.41f,
                                SetValue = false,
                                AffectedAbility = "hasDoubleJump",
                                RevertDirection = CheckDirection.Left
                            },
                            new AbilityModifier()
                            {
                                XPosition = 80.15f,
                                YPosition = 10.3f,
                                SetValue = false,
                                AffectedAbility = "hasDoubleJump",
                                RevertDirection = CheckDirection.Left
                            },
                            new AbilityModifier()
                            {
                                XPosition = 61.54f,
                                YPosition = 7.4f,
                                SetValue = true,
                                AffectedAbility = "hasDoubleJump",
                                RevertDirection = CheckDirection.Left
                            },
                            new AbilityModifier()
                            {
                                XPosition = 198.79f,
                                YPosition = 29.4f,
                                SetValue = true,
                                AffectedAbility = "hasDoubleJump",
                                RevertDirection = CheckDirection.Left
                            },
                        ]
                    },
                    Scene = "Cliffs_02"
                }
    ];
}
