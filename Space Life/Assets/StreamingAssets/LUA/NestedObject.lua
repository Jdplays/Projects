-- TODO: Figure out the nicest way to have unified defines/enums
-- between C# and Lua so we don't have to duplicate anything.
ENTERABILITY_YES = 0
ENTERABILITY_NO = 1
ENTERABILITY_SOON = 2

-- HOWTO Log:
-- ModUtils.ULog("Testing ModUtils.ULogChannel")
-- ModUtils.ULogWarning("Testing ModUtils.ULogWarningChannel")
-- ModUtils.ULogError("Testing ModUtils.ULogErrorChannel") -- Note: pauses the game

-------------------------------- nestedObject Actions --------------------------------

function OnUpdate_Door( nestedObject, deltaTime )
	if (nestedObject.Parameters["is_opening"].ToFloat() >= 1.0) then
		nestedObject.Parameters["openness"].ChangeFloatValue(deltaTime * 4) -- FIXME: Maybe a door open speed parameter?
		if (nestedObject.Parameters["openness"].ToFloat() >= 1)  then
			nestedObject.Parameters["is_opening"].SetValue(0)
		end
	elseif (nestedObject.Parameters["openness"].ToFloat() > 0.0) then
        nestedObject.Parameters["openness"].ChangeFloatValue(deltaTime * -4)
	end

	nestedObject.Parameters["openness"].SetValue( ModUtils.Clamp01(nestedObject.Parameters["openness"].ToFloat()) )

	if (nestedObject.verticalDoor == true) then
		nestedObject.SetAnimationState("vertical")
	else
		nestedObject.SetAnimationState("horizontal")
	end
    nestedObject.SetAnimationProgressValue(nestedObject.Parameters["openness"].ToFloat(), 1)
end

function OnUpdate_AirlockDoor( nestedObject, deltaTime )
    OnUpdate_Door(nestedObject, deltaTime)
    return
end

function IsEnterable_AirlockDoor( nestedObject )
    -- If we're not pressure locked we ignore everything else, and act like a normal door
    if (nestedObject.Parameters["pressure_locked"].ToBool() == false) then
        
        nestedObject.Parameters["is_opening"].SetValue(1)

        if (nestedObject.Parameters["openness"].ToFloat() >= 1) then
            return ENTERABILITY_YES --ENTERABILITY.Yes
        end
        
        return ENTERABILITY_SOON --ENTERABILITY.Soon
    else
        local tolerance = 0.005
        local neighbors = nestedObject.Tile.GetNeighbours(false)
        local adjacentRooms = {}
        local pressureEqual = true;
        local count = 0
        for k, tile in pairs(neighbors) do
            if (tile.Room != nil) then
                count = count + 1
                adjacentRooms[count] = tile.Room
            end
        end
        -- Pressure locked but not controlled by an airlock we only open 
        if(nestedObject.Parameters["airlock_controlled"].ToBool() == false) then
            if (ModUtils.Round(adjacentRooms[1].GetTotalGasPressure(),3) == ModUtils.Round(adjacentRooms[2].GetTotalGasPressure(),3)) then
                nestedObject.Parameters["is_opening"].SetValue(1)
                return ENTERABILITY_SOON
            else
            -- I don't think responding with no here actually makes a difference, but let's make the door close immediately just in case
                nestedObject.Parameters["is_opening"].SetValue(0)
                return ENTERABILITY_NO
            end
        else
            if (adjacentRooms[1].HasRoomBehavior("roombehavior_airlock") or adjacentRooms[2].HasRoomBehavior("roombehavior_airlock")) then
                -- Figure out what's inside and what's outside.
                local insideRoom
                local outsideRoom
                if(adjacentRooms[1].HasRoomBehavior("roombehavior_airlock")) then
                    insideRoom = adjacentRooms[1]
                    outsideRoom = adjacentRooms[2]
                else
                    insideRoom = adjacentRooms[2]
                    outsideRoom = adjacentRooms[1]
                end
                -- Pressure's different, pump to equalize
                if(math.abs(ModUtils.Round(insideRoom.GetTotalGasPressure(),3) - ModUtils.Round(outsideRoom.GetTotalGasPressure(),3)) > tolerance) then
                    if (insideRoom.GetTotalGasPressure() < outsideRoom.GetTotalGasPressure()) then
                        insideRoom.RoomBehaviors["roombehavior_airlock"].CallEventAction("PumpIn",  outsideRoom.GetTotalGasPressure())
                    else
                        insideRoom.RoomBehaviors["roombehavior_airlock"].CallEventAction("PumpOut", outsideRoom.GetTotalGasPressure())
                    end
                    return ENTERABILITY_SOON
                else
                    if (nestedObject.Parameters["openness"].ToFloat() >= 1) then
                        -- We're fully open deactivate pumps and let the room know we're done pumping
                        insideRoom.RoomBehaviors["roombehavior_airlock"].CallEventAction("PumpOff")
                        return ENTERABILITY_YES --ENTERABILITY.Yes
                    end
                    nestedObject.Parameters["is_opening"].SetValue(1)
                    return ENTERABILITY_SOON --ENTERABILITY.Soon
                end
            end
        end
    end
end


function AirlockDoor_Toggle_Pressure_Lock(nestedObject, character)

    ModUtils.ULog("Toggling Pressure Lock")
    
	nestedObject.Parameters["pressure_locked"].SetValue(not nestedObject.Parameters["pressure_locked"].ToBool())
    
    ModUtils.ULog(nestedObject.Parameters["pressure_locked"].ToBool())
end


function OnUpdate_Leak_Door( nestedObject, deltaTime )
	nestedObject.Tile.EqualiseGas(deltaTime * 10.0 * (nestedObject.Parameters["openness"].ToFloat() + 0.1))
end

function OnUpdate_Leak_Airlock( nestedObject, deltaTime )
	nestedObject.Tile.EqualiseGas(deltaTime * 10.0 * (nestedObject.Parameters["openness"].ToFloat()))
end

function IsEnterable_Door( nestedObject )
	nestedObject.Parameters["is_opening"].SetValue(1)

	if (nestedObject.Parameters["openness"].ToFloat() >= 1) then
		return ENTERABILITY_YES --ENTERABILITY.Yes
	end

    return ENTERABILITY_SOON --ENTERABILITY.Soon
end

function Stockpile_GetItemsFromFilter( nestedObject )
	-- TODO: This should be reading from some kind of UI for this
	-- particular stockpile

    -- Probably, this doesn't belong in Lua at all and instead we should
    -- just be calling a C# function to give us the list.

    -- Since jobs copy arrays automatically, we could already have
    -- an Inventory[] prepared and just return that (as a sort of example filter)

	--return { Inventory.__new("Steel Plate", 50, 0) }
	return nestedObject.AcceptsForStorage()
end

function Stockpile_UpdateAction( nestedObject, deltaTime )
    -- We need to ensure that we have a job on the queue
    -- asking for either:
    -- (if we are empty): That ANY loose inventory be brought to us.
    -- (if we have something): Then IF we are still below the max stack size,
    -- that more of the same should be brought to us.

    -- TODO: This function doesn't need to run each update. Once we get a lot
    -- of nestedObject in a running game, this will run a LOT more than required.
    -- Instead, it only really needs to run whenever:
    -- -- It gets created
    -- -- A good gets delivered (at which point we reset the job)
    -- -- A good gets picked up (at which point we reset the job)
    -- -- The UI's filter of allowed items gets changed

    if( nestedObject.Tile.Inventory != nil and nestedObject.Tile.Inventory.StackSize >= nestedObject.Tile.Inventory.MaxStackSize ) then
        -- We are full!
        nestedObject.Jobs.CancelAll()
        return
    end

    -- Maybe we already have a job queued up?
    if( nestedObject.Jobs.Count > 0 ) then
        -- Cool, all done.
        return
    end

    -- We Currently are NOT full, but we don't have a job either.
    -- Two possibilities: Either we have SOME inventory, or we have NO inventory.

    -- Third possibility: Something is WHACK
    if( nestedObject.Tile.Inventory != nil and nestedObject.Tile.Inventory.StackSize == 0 ) then
        nestedObject.Jobs.CancelAll()
        return "Stockpile has a zero-size stack. This is clearly WRONG!"
    end


  	-- TODO: In the future, stockpiles -- rather than being a bunch of individual
  	-- 1x1 tiles -- should manifest themselves as single, large objects.  This
  	-- would respresent our first and probably only VARIABLE sized "nestedObject" --
  	-- at what happenes if there's a "hole" in our stockpile because we have an
  	-- actual piece of nestedObject (like a cooking stating) installed in the middle
  	-- of our stockpile?
  	-- In any case, once we implement "mega stockpiles", then the job-creation system
  	-- could be a lot smarter, in that even if the stockpile has some stuff in it, it
  	-- can also still be requestion different object types in its job creation.

    local itemsDesired = {}

	if( nestedObject.Tile.Inventory == nil ) then
		--ModUtils.ULog("Creating job for new stack.")
		itemsDesired = Stockpile_GetItemsFromFilter( nestedObject )
	else
		--ModUtils.ULog("Creating job for existing stack.")
		local inventory = nestedObject.Tile.Inventory
		local item = RequestedItem.__new(inventory.Type, 1, inventory.MaxStackSize - inventory.StackSize)
        itemsDesired = { item }
    end

  	local job = Job.__new(
    		nestedObject.Tile,
    		"Stockpile_UpdateAction",
    		nil,
    		0,
    		itemsDesired,
    		Job.JobPriority.Low,
    		false
  	)
  	job.JobDescription = "job_stockpile_moving_desc"
  	job.acceptsAny = true

  	-- TODO: Later on, add stockpile priorities, so that we can take from a lower
  	-- priority stockpile for a higher priority one.
  	job.canTakeFromStockpile = false

  	job.RegisterJobWorkedCallback("Stockpile_JobWorked")
  	nestedObject.Jobs.Add(job)
end

function Stockpile_JobWorked(job)
    job.CancelJob()

    -- TODO: Change this when we figure out what we're doing for the all/any pickup job.
    --values = job.GetInventoryRequirementValues();
    for k, inv in pairs(job.HeldInventory) do
        if(inv.StackSize > 0) then
            World.Current.inventoryManager.PlaceInventory(job.tile, inv)
            return -- There should be no way that we ever end up with more than on inventory requirement with StackSize > 0
        end
    end
end

function MiningDroneControl_UpdateAction( nestedObject )
    if(nestedObject.GetTotalInternalInventory() >= 500 and nestedObject.Parameters["mine_complete"].Value == "false") then
        WorldController.Instance.DroneController.MiningComplete(nestedObject)
        nestedObject.Status = "Transporting"
        return
    end

    if (nestedObject.jobs.count > 0) then
        return
    end

    if (nestedObject.GetTotalInternalInventory() < 500) then
        nestedObject.Status = "Mining"

        local job = Job.__new(
        nestedObject.Jobs.WorkSpotTile,
        "MiningDroneControl_UpdateAction",
        nil,
        1,
        nil,
        Job.JobPriority.Medium,
        false
        )
  		
  		job.JobDescription = "Operating the mining drone."

        job.RegisterJobWorkedCallback("MiningDroneControl_JobWorked")
        nestedObject.Jobs.Add(job)
        return
    end
end

function MiningDroneControl_JobWorked(job)
    job.CancelJob()
    if (job.tile.nestedObject.DoesInternalInventoryContain(job.tile.nestedObject.Parameters["mine_type"]) == false) then
        job.tile.nestedObject.CreateInternalInventoryList(job.tile.nestedObject.Parameters["mine_type"].ToString())
    end

    job.tile.nestedObject.AddToInternalInventory(job.tile.nestedObject.Parameters["mine_type"], job.tile.nestedObject.GetIndexOfFreeInternalInventory(job.tile.nestedObject.Parameters["mine_type"], 1), 1)
end

function MiningDroneControl_Change_to_Raw_Iron( nestedObject, character )
	nestedObject.Parameters["mine_type"].SetValue("Raw Iron")
end

function MiningDroneControl_Change_to_Raw_Copper( nestedObject, character )
	nestedObject.Parameters["mine_type"].SetValue("Raw Copper")
end

function LandingPad_Test_CallTradeShip(nestedObject, character)
   WorldController.Instance.TradeController.CallTradeShipTest(nestedObject)
end

function MetalSmelter_UpdateAction(nestedObject, deltaTime)
    local inputSpot = nestedObject.Jobs.InputSpotTile
    local outputSpot = nestedObject.Jobs.OutputSpotTile

    if (inputSpot.Inventory ~= nil and inputSpot.Inventory.StackSize >= 2) then
        nestedObject.Parameters["smelttime"].ChangeFloatValue(deltaTime)
        if (nestedObject.Parameters["smelttime"].ToFloat() >= nestedObject.Parameters["smelttime_required"].ToFloat()) then
            nestedObject.Parameters["smelttime"].SetValue(0)

            if (outputSpot.Inventory == nil) then
                World.Current.inventoryManager.PlaceInventory(outputSpot, Inventory.__new(nestedObject.Parameters["smelt_result"].ToString(), 1))
                inputSpot.Inventory.StackSize = inputSpot.Inventory.StackSize - 2

            elseif (outputSpot.Inventory.StackSize <= outputSpot.Inventory.MaxStackSize - 1) then
                outputSpot.Inventory.StackSize = outputSpot.Inventory.StackSize + 1
                inputSpot.Inventory.StackSize = inputSpot.Inventory.StackSize - 2
            end

            if (inputSpot.Inventory.StackSize <= 0) then
                inputSpot.Inventory = nil
            end
        end
    end

    if (inputSpot.Inventory ~= nil and inputSpot.Inventory.StackSize == inputSpot.Inventory.MaxStackSize) then
        -- We have the max amount of resources, cancel the job.
        -- This check exists mainly, because the job completed callback doesn't
        -- seem to be reliable.
        nestedObject.Jobs.CancelAll()
        return
    end

    if (nestedObject.Jobs.Count > 0) then
        return
    end

    if (inputSpot.Inventory ~= nil and inputSpot.Inventory.Type ~= nestedObject.Parameters["smelt_input"].ToString()) then
        return
    end

    -- Create job depending on the already available stack size.
    local desiredStackSize = 50
    if(inputSpot.Inventory ~= nil and inputSpot.Inventory.StackSize < inputSpot.Inventory.MaxStackSize) then
        desiredStackSize = inputSpot.Inventory.MaxStackSize - inputSpot.Inventory.StackSize
    end
    local itemsDesired = { RequestedItem.__new(nestedObject.Parameters["smelt_input"].ToString(), desiredStackSize) }
    ModUtils.ULog("MetalSmelter: Creating job for " .. desiredStackSize .. nestedObject.Parameters["smelt_input"].ToString())

    local job = Job.__new(
        nestedObject.Jobs.WorkSpotTile,
        "MetalSmelter_UpdateAction",
        nil,
        0.4,
        itemsDesired,
        Job.JobPriority.Medium,
        false
    )

    job.RegisterJobWorkedCallback("MetalSmelter_JobWorked")
    nestedObject.Jobs.Add(job)
    return
end

function MetalSmelter_JobWorked(job)
    job.CancelJob()
    local inputSpot = job.tile.nestedObject.Jobs.InputSpotTile
    for k, inv in pairs(job.HeldInventory) do
        if(inv ~= nil and inv.StackSize > 0) then
            World.Current.inventoryManager.PlaceInventory(inputSpot, inv)
            inputSpot.Inventory.Locked = true
            return
        end
    end
end

function MetalSmelter_Change_to_Iron( nestedObject, character )
    nestedObject.Jobs.CancelAll()
    nestedObject.Jobs.InputSpotTile.Locked = false
    nestedObject.Parameters["smelt_result"].SetValue("Iron Ingot")
    nestedObject.Parameters["smelt_input"].SetValue("Raw Iron")
end

function MetalSmelter_Change_to_Copper( nestedObject, character )
    nestedObject.Jobs.CancelAll()
    nestedObject.Jobs.InputSpotTile.Locked = false
    nestedObject.Parameters["smelt_result"].SetValue("Copper Ingot")
    nestedObject.Parameters["smelt_input"].SetValue("Raw Copper")
end

function SolarPanel_OnUpdate(nestedObject, deltaTime)
    local baseOutput = nestedObject.Parameters["base_output"].ToFloat()
    local efficiency = nestedObject.Parameters["efficiency"].ToFloat()
    local powerPerSecond = baseOutput * efficiency
    nestedObject.PowerConnection.OutputRate = powerPerSecond
end

ModUtils.ULog("nestedObject.lua loaded")
return "LUA Script Parsed!"
