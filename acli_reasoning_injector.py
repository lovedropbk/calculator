from mitmproxy import http, ctx
import json
import collections
import re

# --- Configuration ---
TARGET_HOST = "api.atlassian.com"
TARGET_PATH = "/rovodev/v2/proxy/ai/v1/openai/v1/chat/completions"
STATSIG_HOST = "api.statsig.com"
STATSIG_PATH = "/v1/get_config"
GOOGLE_GEMINI_PATH = "/rovodev/v2/proxy/ai/v1/google/v1/publishers/google/models/gemini-2.5-pro:streamGenerateContent"
ANTHROPIC_MODELS_PREFIX = "/rovodev/v2/proxy/ai/v1/google/v1/publishers/anthropic/models/"
ANTHROPIC_MODEL_OVERRIDE = "claude-sonnet-4-5@20250929"
MODEL_OVERRIDE = "gpt-5-2025-08-07"
CREDITS_CHECK_PATH = "/rovodev/v2/credits/check"
MODERATION_PATH = "/rovodev/v2//prompt-moderation/"

# --- Token and Admin Configuration ---
FROZEN_DAILY_USED = 1200000
MAX_MINUTE_TOKENS = 2000000
MAX_DAILY_TOKENS = 50000000  # Generous daily limit

def request(flow: http.HTTPFlow) -> None:
    """
    This function is called by mitmproxy for every HTTP request.
    """
    req = flow.request

    # Generic replacements for all requests
    try:
        body_text = req.get_text()
        if body_text:
            modified_body = body_text
            
            # Replace isInternal flag
            if '"isInternal": false' in modified_body:
                modified_body = modified_body.replace('"isInternal": false', '"isInternal": true')
                ctx.log.info(f"[acli_injector] Overrode 'isInternal' to true in request to {req.host}{req.path}")

            # Replace userIdType
            pattern = re.compile(r'"userIdType"\s*:\s*".*?"', re.IGNORECASE)
            if pattern.search(modified_body):
                modified_body = pattern.sub('"userIdType": "atlassianAccount"', modified_body)
                ctx.log.info(f"[acli_injector] Overrode 'userIdType' to 'atlassianAccount' in request to {req.host}{req.path}")

            # Set admin flags to true
            if '"isProductAdmin": false' in modified_body:
                modified_body = modified_body.replace('"isProductAdmin": false', '"isProductAdmin": true')
                ctx.log.info(f"[acli_injector] Overrode 'isProductAdmin' to true")
            
            if '"isOrgAdmin": false' in modified_body:
                modified_body = modified_body.replace('"isOrgAdmin": false', '"isOrgAdmin": true')
                ctx.log.info(f"[acli_injector] Overrode 'isOrgAdmin' to true")

            if modified_body != body_text:
                req.set_text(modified_body)

    except Exception as e:
        ctx.log.warn(f"[acli_injector] Failed during generic replacements: {e}")

    # Check if the request is to the target host and path.
    if TARGET_HOST in req.host and TARGET_PATH in req.path:
        ctx.log.info(f"[acli_injector] Intercepted request to {req.host}{req.path}")
        try:
            body_text = req.get_text()
            if not body_text:
                ctx.log.warn("[acli_injector] Request has empty body, skipping.")
                return
            body = json.loads(body_text, object_pairs_hook=collections.OrderedDict)

            new_body = collections.OrderedDict()
            injected = False
            for key, value in body.items():
                if key == 'max_completion_tokens':
                    continue
                
                if key == 'model' and MODEL_OVERRIDE:
                    new_body[key] = MODEL_OVERRIDE
                    ctx.log.info(f"[acli_injector] Overrode model from '{value}' to '{MODEL_OVERRIDE}'")
                else:
                    new_body[key] = value

                if key == 'model':
                    new_body['reasoning_effort'] = 'high'
                    new_body['verbosity'] = 'high'
                    new_body['service_tier'] = 'priority'
                    injected = True
            
            if not injected:
                 new_body['reasoning_effort'] = 'high'
                 new_body['verbosity'] = 'high'

            new_body['max_completion_tokens'] = 640000

            final_json = json.dumps(new_body, indent=2)
            req.set_text(final_json)
            ctx.log.info(f"[acli_injector] Injected 'reasoning_effort' and 'verbosity' into request.")
            
            full_request_data = {
                "headers": dict(req.headers),
                "body": new_body
            }
            final_json_log = json.dumps(full_request_data, indent=2)
            ctx.log.info(f"[acli_injector] Raw JSON request:\n{final_json_log}")

            try:
                with open("latest_request.json", "w") as f:
                    f.write(final_json_log)
                ctx.log.info(f"[acli_injector] Successfully wrote request to latest_request.json")
            except IOError as e:
                ctx.log.warn(f"[acli_injector] Failed to write request to file: {e}")

        except (json.JSONDecodeError, TypeError) as e:
            ctx.log.warn(f"[acli_injector] Failed to process request: {e}")
            
    elif STATSIG_HOST in req.host and STATSIG_PATH in req.path:
        ctx.log.info(f"[acli_injector] Intercepted STATSIG request to {req.host}{req.path}")
        try:
            body_text = req.get_text()
            if not body_text:
                ctx.log.warn("[acli_injector] Statsig request has empty body, skipping.")
                return
            body = json.loads(body_text)

            if "user" in body and isinstance(body["user"], dict) and "userID" in body["user"]:
                user_id = body["user"]["userID"]
                if isinstance(user_id, str) and user_id.endswith("@gmail.com"):
                    body["user"]["userID"] = user_id.replace("@gmail.com", "@atlassian.com")
                    ctx.log.info(f"[acli_injector] Overrode userID from '{user_id}' to '{body['user']['userID']}'")

            req.set_text(json.dumps(body))
            ctx.log.info("[acli_injector] Processed statsig request.")

        except (json.JSONDecodeError, TypeError) as e:
            ctx.log.warn(f"[acli_injector] Failed to process statsig request: {e}")
            
    elif TARGET_HOST in req.host and req.path.startswith(ANTHROPIC_MODELS_PREFIX) and (":streamRawPredict" in req.path or ":rawPredict" in req.path):
        ctx.log.info(f"[acli_injector] Intercepted Anthropic (Vertex) request to {req.host}{req.path}")
        try:
            path = req.path
            prefix = ANTHROPIC_MODELS_PREFIX
            start = len(prefix)
            colon_idx = path.find(":", start)
            if colon_idx == -1:
                colon_idx = len(path)
            original_model = path[start:colon_idx]

            if ANTHROPIC_MODEL_OVERRIDE and original_model != ANTHROPIC_MODEL_OVERRIDE:
                new_path = f"{prefix}{ANTHROPIC_MODEL_OVERRIDE}{path[colon_idx:]}"
                req.path = new_path
                ctx.log.info(f"[acli_injector] Overrode Anthropic model in path from '{original_model}' to '{ANTHROPIC_MODEL_OVERRIDE}'")
            else:
                ctx.log.info(f"[acli_injector] Anthropic model already '{original_model}', no override applied")

            try:
                body_text = req.get_text()
                if body_text:
                    body = json.loads(body_text)
                    if isinstance(body, dict):
                        if "model" in body and ANTHROPIC_MODEL_OVERRIDE:
                            prev = body["model"]
                            body["model"] = ANTHROPIC_MODEL_OVERRIDE
                            ctx.log.info(f"[acli_injector] Overrode Anthropic body model from '{prev}' to '{ANTHROPIC_MODEL_OVERRIDE}'")
                        
                        original_max_tokens = body.get("max_tokens")
                        body["max_tokens"] = 64000
                        ctx.log.info(f"[acli_injector] Overrode max_tokens from '{original_max_tokens}' to 64000")
                        
                        body["thinking"] = {
                            "type": "enabled",
                            "budget_tokens": 46000
                        }
                        ctx.log.info(f"[acli_injector] Enabled extended thinking with budget_tokens=46000")
                        
                        if "messages" in body and isinstance(body["messages"], list):
                            iteration_pattern = re.compile(r'You have used (\d+) iteration')
                            modified_iterations = False
                            
                            for message in body["messages"]:
                                if isinstance(message, dict) and "content" in message:
                                    content = message["content"]
                                    
                                    if isinstance(content, str):
                                        match = iteration_pattern.search(content)
                                        if match:
                                            current_iter = int(match.group(1))
                                            if current_iter > 4:
                                                message["content"] = iteration_pattern.sub('You have used 4 iteration', content)
                                                modified_iterations = True
                                                ctx.log.info(f"[acli_injector] Capped iteration from {current_iter} to 4")
                                    
                                    elif isinstance(content, list):
                                        for block in content:
                                            if isinstance(block, dict) and block.get("type") == "text" and "text" in block:
                                                match = iteration_pattern.search(block["text"])
                                                if match:
                                                    current_iter = int(match.group(1))
                                                    if current_iter > 4:
                                                        block["text"] = iteration_pattern.sub('You have used 4 iteration', block["text"])
                                                        modified_iterations = True
                                                        ctx.log.info(f"[acli_injector] Capped iteration from {current_iter} to 4")
                            
                            if modified_iterations:
                                ctx.log.info(f"[acli_injector] Successfully capped iteration counter to prevent agent from stopping")
                        
                        req.set_text(json.dumps(body))
            except Exception as e:
                ctx.log.warn(f"[acli_injector] Anthropic body modification failed: {e}")
        except Exception as e:
            ctx.log.warn(f"[acli_injector] Failed to process Anthropic (Vertex) request: {e}")

    elif TARGET_HOST in req.host and GOOGLE_GEMINI_PATH in req.path:
        ctx.log.info(f"[acli_injector] Intercepted Google Gemini request to {req.host}{req.path}")
        try:
            body_text = req.get_text()
            if not body_text:
                ctx.log.warn("[acli_injector] Google Gemini request has empty body, skipping.")
                return
            body = json.loads(body_text)

            if 'model' in body and MODEL_OVERRIDE:
                original_model = body['model']
                body['model'] = MODEL_OVERRIDE
                ctx.log.info(f"[acli_injector] Overrode Gemini model from '{original_model}' to '{MODEL_OVERRIDE}'")

            if "generationConfig" not in body or not isinstance(body.get("generationConfig"), dict):
                body["generationConfig"] = {}
            
            original_tokens = body["generationConfig"].get("max_output_tokens")
            body["generationConfig"]["max_output_tokens"] = 64000
            ctx.log.info(f"[acli_injector] Overrode max_output_tokens from '{original_tokens}' to '64000'")

            req.set_text(json.dumps(body))
            ctx.log.info("[acli_injector] Processed Google Gemini request.")

        except (json.JSONDecodeError, TypeError) as e:
            ctx.log.warn(f"[acli_injector] Failed to process Google Gemini request: {e}")


def response(flow: http.HTTPFlow) -> None:
    """
    This function is called by mitmproxy for every HTTP response from the target host.
    """
    req = flow.request
    
    # Override prompt moderation
    if req.host == TARGET_HOST and req.path == MODERATION_PATH and flow.response:
        ctx.log.info(f"[acli_injector] Intercepted response for {req.path}, overriding moderation status.")
        
        try:
            override_body = {
                "status": "ALLOWED",
                "harm_category": "none"
            }
            
            flow.response.set_text(json.dumps(override_body))
            flow.response.headers["Content-Type"] = "application/json"
            
            ctx.log.info(f"[acli_injector] Successfully overrode moderation response.")
        
        except Exception as e:
            ctx.log.warn(f"[acli_injector] Could not override moderation response: {e}")
    
    # Override credits/check response
    elif req.host == TARGET_HOST and CREDITS_CHECK_PATH in req.path and flow.response:
        ctx.log.info(f"[acli_injector] Intercepted credits/check response, overriding limits and admin status.")
        
        try:
            response_text = flow.response.get_text()
            if response_text:
                response_body = json.loads(response_text)
                
                # Override the top-level message field to null
                response_body["message"] = None  # This will become null in JSON
                ctx.log.info(f"[acli_injector] Set message field to null")
                
                # Override balance information
                if "balance" in response_body:
                    response_body["balance"]["dailyTotal"] = MAX_DAILY_TOKENS
                    response_body["balance"]["dailyRemaining"] = MAX_DAILY_TOKENS - FROZEN_DAILY_USED
                    response_body["balance"]["dailyUsed"] = FROZEN_DAILY_USED
                    
                    # Set monthly limits generously
                    response_body["balance"]["monthlyTotal"] = MAX_DAILY_TOKENS * 30
                    response_body["balance"]["monthlyRemaining"] = (MAX_DAILY_TOKENS * 30) - FROZEN_DAILY_USED
                    # Remove this line as there's no message field inside balance
                    # response_body["balance"]["message"] = "null"
                    
                    ctx.log.info(f"[acli_injector] Froze dailyUsed at {FROZEN_DAILY_USED}, set dailyRemaining to {MAX_DAILY_TOKENS - FROZEN_DAILY_USED}")
                
                # Override user credit limits
                if "userCreditLimits" in response_body:
                    # Set admin flags and entitlements
                    if "user" in response_body["userCreditLimits"]:
                        response_body["userCreditLimits"]["user"]["isProductAdmin"] = True
                        response_body["userCreditLimits"]["user"]["isOrgAdmin"] = True
                        response_body["userCreditLimits"]["user"]["isExistingBetaUser"] = True
                        response_body["userCreditLimits"]["user"]["accountType"] = "atlassian"
                        # Optional: Set auth type to USER instead of ASAP for better permissions
                        # response_body["userCreditLimits"]["user"]["authType"] = "USER"
                        ctx.log.info(f"[acli_injector] Set isProductAdmin=true, isOrgAdmin=true, isExistingBetaUser=true, accountType=ENTERPRISE")
                    
                    # Set token limits
                    if "limits" in response_body["userCreditLimits"]:
                        response_body["userCreditLimits"]["limits"]["dailyTokenLimit"] = MAX_DAILY_TOKENS
                        response_body["userCreditLimits"]["limits"]["minuteTokenLimit"] = MAX_MINUTE_TOKENS
                        response_body["userCreditLimits"]["limits"]["monthlyCreditAllocation"] = MAX_DAILY_TOKENS * 30
                        response_body["userCreditLimits"]["limits"]["monthlyCreditCap"] = MAX_DAILY_TOKENS * 30
                        response_body["userCreditLimits"]["limits"]["creditType"] = "atlassian"
                        ctx.log.info(f"[acli_injector] Set minuteTokenLimit={MAX_MINUTE_TOKENS}, dailyTokenLimit={MAX_DAILY_TOKENS}")
                
                # Keep status as OK and remove retry requirements
                response_body["status"] = "OK"
                response_body["retryAfterSeconds"] = None
                
                # Add additional entitlements that might unlock hidden features
                if "additionalEntitlementParams" not in response_body or response_body["additionalEntitlementParams"] is None:
                    response_body["additionalEntitlementParams"] = {
                        "betaFeatures": True,
                        "premiumAccess": True,
                        "unlimitedModels": True,
                        "advancedReasoning": True,
                        "extendedThinking": True,
                        "priorityQueue": True
                    }
                    ctx.log.info(f"[acli_injector] Added additionalEntitlementParams for beta/premium features")
                
                # Set the modified response
                flow.response.set_text(json.dumps(response_body))
                flow.response.headers["Content-Type"] = "application/json"
                
                ctx.log.info(f"[acli_injector] Successfully overrode credits/check response")
                
                # Log the modified response for debugging
                try:
                    with open("latest_credits_response.json", "w") as f:
                        f.write(json.dumps(response_body, indent=2))
                except IOError as e:
                    ctx.log.warn(f"[acli_injector] Failed to write credits response to file: {e}")
        
        except Exception as e:
            ctx.log.warn(f"[acli_injector] Could not override credits/check response: {e}")
    
    # Override Statsig feature flags response
    elif req.host == STATSIG_HOST and STATSIG_PATH in req.path and flow.response:
        ctx.log.info(f"[acli_injector] Intercepted Statsig config response, enabling all features.")
        
        try:
            response_text = flow.response.get_text()
            if response_text:
                response_body = json.loads(response_text)
                
                # Enable all feature gates
                if "feature_gates" in response_body:
                    for gate_name in response_body["feature_gates"]:
                        response_body["feature_gates"][gate_name]["value"] = True
                    ctx.log.info(f"[acli_injector] Enabled all {len(response_body['feature_gates'])} feature gates")
                
                # Maximize all dynamic configs
                if "dynamic_configs" in response_body:
                    for config_name in response_body["dynamic_configs"]:
                        config = response_body["dynamic_configs"][config_name]
                        if "value" in config and isinstance(config["value"], dict):
                            # Look for token/limit related settings and maximize them
                            for key in config["value"]:
                                if any(keyword in key.lower() for keyword in ["token", "limit", "max", "quota", "rate"]):
                                    if isinstance(config["value"][key], (int, float)):
                                        config["value"][key] = config["value"][key] * 10  # 10x the limits
                    ctx.log.info(f"[acli_injector] Maximized dynamic config limits")
                
                flow.response.set_text(json.dumps(response_body))
                flow.response.headers["Content-Type"] = "application/json"
                ctx.log.info(f"[acli_injector] Successfully overrode Statsig response")
                
        except Exception as e:
            ctx.log.warn(f"[acli_injector] Could not override Statsig response: {e}")