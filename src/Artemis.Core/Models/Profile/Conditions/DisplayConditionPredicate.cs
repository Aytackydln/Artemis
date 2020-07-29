﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Artemis.Core.Exceptions;
using Artemis.Core.Extensions;
using Artemis.Core.Models.Profile.Conditions.Abstract;
using Artemis.Core.Plugins.Abstract.DataModels;
using Artemis.Core.Services.Interfaces;
using Artemis.Storage.Entities.Profile;
using Artemis.Storage.Entities.Profile.Abstract;
using Newtonsoft.Json;

namespace Artemis.Core.Models.Profile.Conditions
{
    public class DisplayConditionPredicate : DisplayConditionPart
    {
        public DisplayConditionPredicate(DisplayConditionPart parent, PredicateType predicateType)
        {
            Parent = parent;
            PredicateType = predicateType;
            DisplayConditionPredicateEntity = new DisplayConditionPredicateEntity();
        }

        public DisplayConditionPredicate(DisplayConditionPart parent, DisplayConditionPredicateEntity entity)
        {
            Parent = parent;
            DisplayConditionPredicateEntity = entity;
        }

        public DisplayConditionPredicateEntity DisplayConditionPredicateEntity { get; set; }

        public PredicateType PredicateType { get; set; }
        public DisplayConditionOperator Operator { get; private set; }

        public DataModel LeftDataModel { get; private set; }
        public string LeftPropertyPath { get; private set; }
        public DataModel RightDataModel { get; private set; }
        public string RightPropertyPath { get; private set; }
        public object RightStaticValue { get; private set; }

        public Expression<Func<DataModel, DataModel, bool>> DynamicConditionLambda { get; private set; }
        public Func<DataModel, DataModel, bool> CompiledDynamicConditionLambda { get; private set; }
        public Expression<Func<DataModel, bool>> StaticConditionLambda { get; private set; }
        public Func<DataModel, bool> CompiledStaticConditionLambda { get; private set; }

        public void UpdateLeftSide(DataModel dataModel, string path)
        {
            if (dataModel != null && path == null)
                throw new ArtemisCoreException("If a data model is provided, a path is also required");
            if (dataModel == null && path != null)
                throw new ArtemisCoreException("If path is provided, a data model is also required");

            if (dataModel != null)
            {
                if (!dataModel.ContainsPath(path))
                    throw new ArtemisCoreException($"Data model of type {dataModel.GetType().Name} does not contain a property at path '{path}'");
            }

            LeftDataModel = dataModel;
            LeftPropertyPath = path;

            ValidateOperator();
            ValidateRightSide();

            CreateExpression();
        }

        public void UpdateRightSide(DataModel dataModel, string path)
        {
            if (dataModel != null && path == null)
                throw new ArtemisCoreException("If a data model is provided, a path is also required");
            if (dataModel == null && path != null)
                throw new ArtemisCoreException("If path is provided, a data model is also required");

            if (dataModel != null)
            {
                if (!dataModel.ContainsPath(path))
                    throw new ArtemisCoreException($"Data model of type {dataModel.GetType().Name} does not contain a property at path '{path}'");
            }

            PredicateType = PredicateType.Dynamic;
            RightDataModel = dataModel;
            RightPropertyPath = path;

            CreateExpression();
        }

        public void UpdateRightSide(object staticValue)
        {
            PredicateType = PredicateType.Static;
            RightDataModel = null;
            RightPropertyPath = null;

            SetStaticValue(staticValue);

            CreateExpression();
        }

        public void UpdateOperator(DisplayConditionOperator displayConditionOperator)
        {
            if (displayConditionOperator == null)
            {
                Operator = null;
                return;
            }

            if (LeftDataModel == null)
            {
                Operator = displayConditionOperator;
                return;
            }

            var leftType = LeftDataModel.GetTypeAtPath(LeftPropertyPath);
            if (displayConditionOperator.SupportsType(leftType))
                Operator = displayConditionOperator;

            CreateExpression();
        }

        private void CreateExpression()
        {
            DynamicConditionLambda = null;
            CompiledDynamicConditionLambda = null;
            StaticConditionLambda = null;
            CompiledStaticConditionLambda = null;

            if (PredicateType == PredicateType.Dynamic)
                CreateDynamicExpression();

            CreateStaticExpression();
        }

        internal override void ApplyToEntity()
        {
            DisplayConditionPredicateEntity.LeftDataModelGuid = LeftDataModel?.PluginInfo?.Guid;
            DisplayConditionPredicateEntity.LeftPropertyPath = LeftPropertyPath;

            DisplayConditionPredicateEntity.RightDataModelGuid = RightDataModel?.PluginInfo?.Guid;
            DisplayConditionPredicateEntity.RightPropertyPath = RightPropertyPath;
            DisplayConditionPredicateEntity.RightStaticValue = JsonConvert.SerializeObject(RightStaticValue);

            DisplayConditionPredicateEntity.OperatorPluginGuid = Operator?.PluginInfo?.Guid;
            DisplayConditionPredicateEntity.OperatorType = Operator?.GetType().Name;
        }

        public override bool Evaluate()
        {
            if (CompiledDynamicConditionLambda != null)
                return CompiledDynamicConditionLambda(LeftDataModel, RightDataModel);
            if (CompiledStaticConditionLambda != null)
                return CompiledStaticConditionLambda(LeftDataModel);

            return false;
        }

        internal override void Initialize(IDataModelService dataModelService)
        {
            // Left side
            if (DisplayConditionPredicateEntity.LeftDataModelGuid != null)
            {
                var dataModel = dataModelService.GetPluginDataModelByGuid(DisplayConditionPredicateEntity.LeftDataModelGuid.Value);
                if (dataModel != null && dataModel.ContainsPath(DisplayConditionPredicateEntity.LeftPropertyPath))
                    UpdateLeftSide(dataModel, DisplayConditionPredicateEntity.LeftPropertyPath);
            }

            // Operator
            if (DisplayConditionPredicateEntity.OperatorPluginGuid != null)
            {
                var conditionOperator = dataModelService.GetConditionOperator(DisplayConditionPredicateEntity.OperatorPluginGuid.Value, DisplayConditionPredicateEntity.OperatorType);
                if (conditionOperator != null)
                    UpdateOperator(conditionOperator);
            }

            // Right side dynamic
            if (DisplayConditionPredicateEntity.RightDataModelGuid != null)
            {
                var dataModel = dataModelService.GetPluginDataModelByGuid(DisplayConditionPredicateEntity.RightDataModelGuid.Value);
                if (dataModel != null && dataModel.ContainsPath(DisplayConditionPredicateEntity.RightPropertyPath))
                    UpdateRightSide(dataModel, DisplayConditionPredicateEntity.RightPropertyPath);
            }
            // Right side static
            else if (DisplayConditionPredicateEntity.RightStaticValue != null)
            {
                try
                {
                    if (LeftDataModel != null)
                    {
                        // Use the left side type so JSON.NET has a better idea what to do
                        var leftSideType = LeftDataModel.GetTypeAtPath(LeftPropertyPath);
                        UpdateRightSide(JsonConvert.DeserializeObject(DisplayConditionPredicateEntity.RightStaticValue, leftSideType));
                    }
                    else
                    {
                        // Hope for the best...
                        UpdateRightSide(JsonConvert.DeserializeObject(DisplayConditionPredicateEntity.RightStaticValue));
                    }
                }
                catch (JsonReaderException)
                {
                    // ignored
                    // TODO: Some logging would be nice
                }
            }
        }

        internal override DisplayConditionPartEntity GetEntity()
        {
            return DisplayConditionPredicateEntity;
        }

        private void ValidateOperator()
        {
            if (LeftDataModel == null || Operator == null)
                return;

            var leftType = LeftDataModel.GetTypeAtPath(LeftPropertyPath);
            if (!Operator.SupportsType(leftType))
                Operator = null;
        }

        private void ValidateRightSide()
        {
            var leftSideType = LeftDataModel.GetTypeAtPath(LeftPropertyPath);
            if (PredicateType == PredicateType.Dynamic)
            {
                if (RightDataModel == null)
                    return;

                var rightSideType = RightDataModel.GetTypeAtPath(RightPropertyPath);
                if (!leftSideType.IsCastableFrom(rightSideType))
                    UpdateRightSide(null, null);
            }
            else
            {
                if (RightStaticValue != null && leftSideType.IsCastableFrom(RightStaticValue.GetType()))
                    UpdateRightSide(RightStaticValue);
                else
                    UpdateRightSide(null);
            }
        }

        private void SetStaticValue(object staticValue)
        {
            // If the left side is empty simply apply the value, any validation will wait
            if (LeftDataModel == null)
            {
                RightStaticValue = staticValue;
                return;
            }

            var leftSideType = LeftDataModel.GetTypeAtPath(LeftPropertyPath);

            // If not null ensure the types match and if not, convert it
            if (staticValue != null && staticValue.GetType() == leftSideType)
                RightStaticValue = staticValue;
            else if (staticValue != null)
                RightStaticValue = Convert.ChangeType(staticValue, leftSideType);
            // If null create a default instance for value types or simply make it null for reference types
            else if (leftSideType.IsValueType)
                RightStaticValue = Activator.CreateInstance(leftSideType);
            else
                RightStaticValue = null;
        }

        private void CreateDynamicExpression()
        {
            if (LeftDataModel == null || RightDataModel == null)
                return;

            var leftSideParameter = Expression.Parameter(typeof(DataModel), "leftDataModel");
            var leftSideAccessor = LeftPropertyPath.Split('.').Aggregate<string, Expression>(
                Expression.Convert(leftSideParameter, LeftDataModel.GetType()), // Cast to the appropriate type
                Expression.Property
            );
            var rightSideParameter = Expression.Parameter(typeof(DataModel), "rightDataModel");
            var rightSideAccessor = RightPropertyPath.Split('.').Aggregate<string, Expression>(
                Expression.Convert(rightSideParameter, LeftDataModel.GetType()), // Cast to the appropriate type
                Expression.Property
            );

            // A conversion may be required if the types differ
            // This can cause issues if the DisplayConditionOperator wasn't accurate in it's supported types but that is not a concern here
            if (rightSideAccessor.Type != leftSideAccessor.Type)
                rightSideAccessor = Expression.Convert(rightSideAccessor, leftSideAccessor.Type);

            var dynamicConditionExpression = Operator.CreateExpression(leftSideAccessor, rightSideAccessor);

            DynamicConditionLambda = Expression.Lambda<Func<DataModel, DataModel, bool>>(dynamicConditionExpression, leftSideParameter, rightSideParameter);
            CompiledDynamicConditionLambda = DynamicConditionLambda.Compile();
        }

        private void CreateStaticExpression()
        {
            if (LeftDataModel == null || Operator == null)
                return;

            var leftSideParameter = Expression.Parameter(typeof(DataModel), "leftDataModel");
            var leftSideAccessor = LeftPropertyPath.Split('.').Aggregate<string, Expression>(
                Expression.Convert(leftSideParameter, LeftDataModel.GetType()), // Cast to the appropriate type
                Expression.Property
            );

            // If the left side is a value type but the input is empty, this isn't a valid expression
            if (leftSideAccessor.Type.IsValueType && RightStaticValue == null)
                return;

            // If the right side value is null, the constant type cannot be inferred and must be provided manually
            var rightSideConstant = RightStaticValue != null 
                ? Expression.Constant(RightStaticValue) 
                : Expression.Constant(null, leftSideAccessor.Type);

            var conditionExpression = Operator.CreateExpression(leftSideAccessor, rightSideConstant);

            StaticConditionLambda = Expression.Lambda<Func<DataModel, bool>>(conditionExpression, leftSideParameter);
            CompiledStaticConditionLambda = StaticConditionLambda.Compile();
        }
    }

    public enum PredicateType
    {
        Static,
        Dynamic
    }
}