duration = 9999
entities = {
    {
    entity_name = "FollowScene",
    type = SCENE,
    template_name = "",
    lt_grp = 0,
    srt_grp = 0,
    usr_flg = 0,
    spatialprops = {
      pos = { 0, 0, 0 },
      orient = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } }
    },
    up = Y_AXIS,
    front = Z_AXIS,
    ambient = { 128, 128, 128 }
  },
    {
    entity_name = "Camera",
    type = CAMERA,
    template_name = "",
    lt_grp = 0,
    srt_grp = 0,
    usr_flg = 0,
    spatialprops = {
      pos = { -10, 7.5, 30 },
      orient = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } }
    },
    cameraprops = {
      fovh = 35,
      hvaspect = 1.333333,
      nearplane = 0.1,
      farplane = 100000
    }
  },
    {
    entity_name = "main_object",
    type = MARKER,
    template_name = "",
    lt_grp = 0,
    srt_grp = 0,
    usr_flg = 0,
    spatialprops = {
      pos = { 0, 0, 0 },
      orient = { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } }
    },
    userprops = {
      main_object = "main_object"
    }
  },
}
events = {
    { 0, SET_CAMERA,     { "Monitor", "Camera" } },
    { 0, ATTACH_ENTITY,     { "Camera", "main_object" }, {
        duration = 9999,
        offset = { 0, 0, 0 },
        target_part = "",
        target_type = ROOT,
        flags = LOOK_AT
    } },
    { 0, ATTACH_ENTITY, { "Camera", "main_object" }, {
        duration = 9999,
        offset = { -10, 7.5, 30 },
        target_part = "",
        target_type = ROOT,
        flags = POSITION
    } },
}
